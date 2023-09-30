using Common.Http;
using Common.Observe;
using Common.Stateful;
using Flurl.Http;
using Garmin.Auth;
using Garmin.Dto;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Garmin
{
	public interface IGarminApiClient
	{
		Task InitCookieJarAsync(object queryParams, string userAgent, out CookieJar jar);
		Task<GarminResult> GetCsrfTokenAsync(GarminApiAuthentication auth, object queryParams, CookieJar jar);
		Task<SendCredentialsResult> SendCredentialsAsync(GarminApiAuthentication auth, object queryParams, object loginData, CookieJar jar);
		Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar);
		Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth);
	}

	public class ApiClient : IGarminApiClient
	{
		private const string SSO_SIGNIN_URL = "https://sso.garmin.com/sso/signin";
		private const string SSO_EMBED_URL = "https://sso.garmin.com/sso/embed";

		private static string UPLOAD_URL = $"https://connectapi.garmin.com/upload-service/upload";

		private const string ORIGIN = "https://sso.garmin.com";
		private const string REFERER = "https://sso.garmin.com/sso/signin";

		private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();

		public Task InitCookieJarAsync(object queryParams, string userAgent, out CookieJar jar)
		{
			return SSO_EMBED_URL
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(out jar)
						.GetStringAsync();
		}

		public async Task<SendCredentialsResult> SendCredentialsAsync(GarminApiAuthentication auth, object queryParams, object loginData, CookieJar jar)
		{
			var result = new SendCredentialsResult();
			result.RawResponseBody = await SSO_SIGNIN_URL
						.WithHeader("User-Agent", auth.UserAgent)
						.WithHeader("origin", ORIGIN)
						.WithHeader("referer", REFERER)
						.WithHeader("NK", "NT")
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.StripSensitiveDataFromLogging(auth.Email, auth.Password)
						.OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
						.PostUrlEncodedAsync(loginData)
						.ReceiveString();

			return result;
		}

		public async Task<GarminResult> GetCsrfTokenAsync(GarminApiAuthentication auth, object queryParams, CookieJar jar)
		{
			var result = new GarminResult();
			result.RawResponseBody = await SSO_SIGNIN_URL
						.WithHeader("User-Agent", auth.UserAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.StripSensitiveDataFromLogging(auth.Email, auth.Password)
						.GetAsync()
						.ReceiveString();

			return result;
		}

		public Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar)
		{
			return "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.WithHeader("referer", "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode")
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.OnRedirect(redir => redir.Request.WithCookies(jar))
						.PostUrlEncodedAsync(mfaData)
						.ReceiveString();
		}

		public async Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth)
		{
			var fileName = Path.GetFileName(filePath);
			var response = await $"{UPLOAD_URL}/{format}"
				.WithOAuthBearerToken(auth.OAuth2Token.Access_Token)
				.WithHeader("NK", "NT")
				.WithHeader("origin", ORIGIN)
				.WithHeader("User-Agent", auth.UserAgent)
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
