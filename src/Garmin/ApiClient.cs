using Common.Http;
using Common.Observe;
using Common.Service;
using Flurl.Http;
using Garmin.Auth;
using Garmin.Dto;
using OAuth;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Garmin
{
	public interface IGarminApiClient
	{
		Task<CookieJar> InitCookieJarAsync(object queryParams);
		Task<GarminResult> GetCsrfTokenAsync(object queryParams, CookieJar jar);
		Task<SendCredentialsResult> SendCredentialsAsync(string email, string password, object queryParams, object loginData, CookieJar jar);
		Task<string> SendMfaCodeAsync(object queryParams, object mfaData, CookieJar jar);
		Task<string> GetOAuth1TokenAsync(ConsumerCredentials credentials, string ticket);
		Task<OAuth2Token> GetOAuth2TokenAsync(OAuth1Token oAuth1Token, ConsumerCredentials credentials);
		Task<ConsumerCredentials> GetConsumerCredentialsAsync();
		Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth);
	}

	public class ApiClient : IGarminApiClient
	{
		private ISettingsService _settingsService;

		private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();

		public ApiClient(ISettingsService settingsService)
		{
			_settingsService = settingsService;
		}

		public Task<ConsumerCredentials> GetConsumerCredentialsAsync()
		{
			return "https://thegarth.s3.amazonaws.com/oauth_consumer.json"
				.GetJsonAsync<ConsumerCredentials>();
		}

		public async Task<CookieJar> InitCookieJarAsync(object queryParams)
		{
			var setttings = await _settingsService.GetSettingsAsync();

			await setttings.Garmin.Api.SsoEmbedUrl
						.WithHeader("User-Agent", setttings.Garmin.Api.SsoUserAgent)
						.WithHeader("origin", setttings.Garmin.Api.Origin)
						.SetQueryParams(queryParams)
						.WithCookies(out var jar)
						.GetStringAsync();

			return jar;
		}

		public async Task<SendCredentialsResult> SendCredentialsAsync(string email, string password, object queryParams, object loginData, CookieJar jar)
		{
			var setttings = await _settingsService.GetSettingsAsync();

			var result = new SendCredentialsResult();
			result.RawResponseBody = await setttings.Garmin.Api.SsoSignInUrl
						.WithHeader("User-Agent", setttings.Garmin.Api.SsoUserAgent)
						.WithHeader("origin", setttings.Garmin.Api.Origin)
						.WithHeader("referer", setttings.Garmin.Api.Referer)
						.WithHeader("NK", "NT")
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.StripSensitiveDataFromLogging(email, password)
						.OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
						.PostUrlEncodedAsync(loginData)
						.ReceiveString();

			return result;
		}

		public async Task<GarminResult> GetCsrfTokenAsync(object queryParams, CookieJar jar)
		{
			var setttings = await _settingsService.GetSettingsAsync();

			var result = new GarminResult();
			result.RawResponseBody = await setttings.Garmin.Api.SsoSignInUrl
						.WithHeader("User-Agent", setttings.Garmin.Api.SsoUserAgent)
						.WithHeader("origin", setttings.Garmin.Api.Origin)
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.GetAsync()
						.ReceiveString();

			return result;
		}

		public async Task<string> SendMfaCodeAsync(object queryParams, object mfaData, CookieJar jar)
		{
			var setttings = await _settingsService.GetSettingsAsync();

			return await setttings.Garmin.Api.SsoMfaCodeUrl
						.WithHeader("User-Agent", setttings.Garmin.Api.SsoUserAgent)
						.WithHeader("origin", setttings.Garmin.Api.Origin)
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.OnRedirect(redir => redir.Request.WithCookies(jar))
						.PostUrlEncodedAsync(mfaData)
						.ReceiveString();
		}

		public async Task<string> GetOAuth1TokenAsync(ConsumerCredentials credentials, string ticket)
		{
			var setttings = await _settingsService.GetSettingsAsync();

			OAuthRequest oauthClient = OAuthRequest.ForRequestToken(credentials.Consumer_Key, credentials.Consumer_Secret);
			oauthClient.RequestUrl = $"{setttings.Garmin.Api.OAuth1TokenUrl}?ticket={ticket}&login-url={setttings.Garmin.Api.OAuth1LoginUrlParam}";

			return await oauthClient.RequestUrl
							.WithHeader("User-Agent", setttings.Garmin.Api.SsoUserAgent)
							.WithHeader("Authorization", oauthClient.GetAuthorizationHeader())
							.GetStringAsync();
		}
		public async Task<OAuth2Token> GetOAuth2TokenAsync(OAuth1Token oAuth1Token, ConsumerCredentials credentials)
		{
			var setttings = await _settingsService.GetSettingsAsync();

			OAuthRequest oauthClient2 = OAuthRequest.ForProtectedResource("POST", credentials.Consumer_Key, credentials.Consumer_Secret, oAuth1Token.Token, oAuth1Token.TokenSecret);
			oauthClient2.RequestUrl = setttings.Garmin.Api.OAuth2RequestUrl;
			
			return await oauthClient2.RequestUrl
								.WithHeader("User-Agent", setttings.Garmin.Api.SsoUserAgent)
								.WithHeader("Authorization", oauthClient2.GetAuthorizationHeader())
								.WithHeader("Content-Type", "application/x-www-form-urlencoded") // this header is required, without it you get a 500
								.PostUrlEncodedAsync(new object()) // hack: PostAsync() will drop the content-type header, by posting empty object we trick flurl into leaving the header
								.ReceiveJson<OAuth2Token>();
		}

		public async Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth)
		{
			var settings = await _settingsService.GetSettingsAsync();

			var fileName = Path.GetFileName(filePath);
			var response = await $"{settings.Garmin.Api.UploadActivityUrl}/{format}"
				.WithOAuthBearerToken(auth.OAuth2Token.Access_Token)
				.WithHeader("NK", settings.Garmin.Api.UplaodActivityNkHeader)
				.WithHeader("origin", settings.Garmin.Api.Origin)
				.WithHeader("User-Agent", settings.Garmin.Api.UploadActivityUserAgent)
				.AllowHttpStatus("2xx,409")
				.PostMultipartAsync((data) =>
				{
					data.AddFile("\"file\"", path: filePath, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");
				})
				.ReceiveJson<UploadResponse>();

			var result = response.DetailedImportResult;

			if (result.Failures.Any())
			{
				foreach (var failure in result.Failures)
				{
					if (failure.Messages.Any())
					{
						foreach (var message in failure.Messages)
						{
							if (message.Code == 202)
							{
								_logger.Information("Activity already uploaded {garminWorkout}", result.FileName);
							}
							else
							{
								_logger.Error("Failed to upload activity to Garmin. Message: {errorMessage}", message);
							}
						}
					}
				}
			}

			return response;
		}
	}
}
