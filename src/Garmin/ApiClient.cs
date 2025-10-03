﻿using Common.Http;
using Common.Observe;
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
		Task InitCookieJarAsync(object queryParams, string userAgent, out CookieJar jar);
		Task<GarminResult> GetCsrfTokenAsync(object queryParams, CookieJar jar, string userAgent);
		Task<SendCredentialsResult> SendCredentialsAsync(string email, string password, object queryParams, object loginData, string userAgent, CookieJar jar);
		Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar);
		Task<string> GetOAuth1TokenAsync(ConsumerCredentials credentials, string ticket, string userAgent);
		Task<OAuth2Token> GetOAuth2TokenAsync(OAuth1Token oAuth1Token, ConsumerCredentials credentials, string userAgent);
		Task<ConsumerCredentials> GetConsumerCredentialsAsync();
		Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth, string userAgent);
	}

	public class ApiClient : IGarminApiClient
	{
		private const string SSO_SIGNIN_URL = "https://sso.garmin.com/sso/signin";
		private const string SSO_EMBED_URL = "https://sso.garmin.com/sso/embed";

		private static string UPLOAD_URL = $"https://connectapi.garmin.com/upload-service/upload";

		private const string ORIGIN = "https://sso.garmin.com";
		private const string REFERER = "https://sso.garmin.com/sso/signin";

		private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();

		public Task<ConsumerCredentials> GetConsumerCredentialsAsync()
		{
			return "https://thegarth.s3.amazonaws.com/oauth_consumer.json"
				.GetJsonAsync<ConsumerCredentials>();
		}

		public Task InitCookieJarAsync(object queryParams, string userAgent, out CookieJar jar)
		{
			return SSO_EMBED_URL
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(out jar)
						.GetStringAsync();
		}

		public async Task<SendCredentialsResult> SendCredentialsAsync(string email, string password, object queryParams, object loginData, string userAgent, CookieJar jar)
		{
			var result = new SendCredentialsResult();
			result.RawResponseBody = await SSO_SIGNIN_URL
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.WithHeader("referer", REFERER)
						.WithHeader("NK", "NT")
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.StripSensitiveDataFromLogging(email, password)
						.OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
						.PostUrlEncodedAsync(loginData)
						.ReceiveString();

			return result;
		}

		public async Task<GarminResult> GetCsrfTokenAsync(object queryParams, CookieJar jar, string userAgent)
		{
			var result = new GarminResult();
			result.RawResponseBody = await SSO_SIGNIN_URL
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.GetAsync()
						.ReceiveString();

			return result;
		}

		public Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar)
		{
			return "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.OnRedirect(redir => redir.Request.WithCookies(jar))
						.PostUrlEncodedAsync(mfaData)
						.ReceiveString();
		}

		public Task<string> GetOAuth1TokenAsync(ConsumerCredentials credentials, string ticket, string userAgent)
		{
			OAuthRequest oauthClient = OAuthRequest.ForRequestToken(credentials.Consumer_Key, credentials.Consumer_Secret);
			oauthClient.RequestUrl = $"https://connectapi.garmin.com/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true";

			return oauthClient.RequestUrl
							.WithHeader("User-Agent", userAgent)
							.WithHeader("Authorization", oauthClient.GetAuthorizationHeader())
							.GetStringAsync();
		}
		public Task<OAuth2Token> GetOAuth2TokenAsync(OAuth1Token oAuth1Token, ConsumerCredentials credentials, string userAgent)
		{
			OAuthRequest oauthClient2 = OAuthRequest.ForProtectedResource("POST", credentials.Consumer_Key, credentials.Consumer_Secret, oAuth1Token.Token, oAuth1Token.TokenSecret);
			oauthClient2.RequestUrl = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";

			return oauthClient2.RequestUrl
								.WithHeader("User-Agent", userAgent)
								.WithHeader("Authorization", oauthClient2.GetAuthorizationHeader())
								.WithHeader("Content-Type", "application/x-www-form-urlencoded") // this header is required, without it you get a 500
								.PostUrlEncodedAsync(new object()) // hack: PostAsync() will drop the content-type header, by posting empty object we trick flurl into leaving the header
								.ReceiveJson<OAuth2Token>();
		}

		public async Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth, string userAgent)
		{
			var fileName = Path.GetFileName(filePath);
			var response = await $"{UPLOAD_URL}/{format}"
				.WithOAuthBearerToken(auth.OAuth2Token.Access_Token)
				.WithHeader("NK", "NT")
				.WithHeader("origin", ORIGIN)
				.WithHeader("User-Agent", userAgent)
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
