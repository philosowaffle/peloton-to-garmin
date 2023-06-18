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
		Task InitSigninFlowAsync(object queryParams, string userAgent, out CookieJar jar);
		Task<SendCredentialsResult> SendCredentialsAsync(GarminApiAuthentication auth, object queryParams, object loginData, CookieJar jar);
		Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar);
		Task<IFlurlResponse> SendServiceTicketAsync(string userAgent, string serviceTicket, CookieJar jar);
		Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth);
	}

	public class ApiClient : IGarminApiClient
	{
		private const string BASE_URL = "https://connect.garmin.com";
		private const string SSO_URL = "https://sso.garmin.com";
		private const string SIGNIN_URL = "https://sso.garmin.com/sso/signin";

		private static string UPLOAD_URL = $"{BASE_URL}/modern/proxy/upload-service/upload";

		private const string ORIGIN = SSO_URL;

		private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();

		public Task InitSigninFlowAsync(object queryParams, string userAgent, out CookieJar jar)
		{
			return SIGNIN_URL
						.WithHeader("User-Agent", userAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(out jar)
						.GetStringAsync();
		}

		public async Task<SendCredentialsResult> SendCredentialsAsync(GarminApiAuthentication auth, object queryParams, object loginData, CookieJar jar)
		{
			var result = new SendCredentialsResult();
			result.RawResponseBody = await SIGNIN_URL
						.WithHeader("User-Agent", auth.UserAgent)
						.WithHeader("origin", ORIGIN)
						.SetQueryParams(queryParams)
						.WithCookies(jar)
						.StripSensitiveDataFromLogging(auth.Email, auth.Password)
						.OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
						.PostUrlEncodedAsync(loginData)
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

		public Task<IFlurlResponse> SendServiceTicketAsync(string userAgent, string serviceTicket, CookieJar jar)
		{
			return $"{BASE_URL}/"
						.WithHeader("User-Agent", userAgent)
						.WithCookies(jar)
						.SetQueryParam("ticket", serviceTicket)
						.GetAsync();
		}

		public async Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth)
		{
			var fileName = Path.GetFileName(filePath);
			var response = await $"{UPLOAD_URL}/{format}"
				.WithCookies(auth.CookieJar)
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

		//private const string URL_HOSTNAME = "https://connect.garmin.com/modern/auth/hostname";
		//private const string URL_LOGIN = "https://sso.garmin.com/sso/login";
		//private const string URL_POST_LOGIN = "https://connect.garmin.com/modern/";
		//private const string URL_HOST_SSO = "sso.garmin.com";
		//private const string URL_HOST_CONNECT = "connect.garmin.com";
		//private const string URL_SSO_SIGNIN = "https://sso.garmin.com/sso/signin";
		//private const string URL_ACTIVITY_BASE = "https://connect.garmin.com/modern/proxy/activity-service/activity";
		//private const string URL_ACTIVITY_TYPES = "https://connect.garmin.com/modern/proxy/activity-service/activity/activityTypes";
		/// <summary>
		/// This is where the magic happens!
		/// Straight from  https://github.com/La0/garmin-uploader
		/// </summary>
		//public async Task InitAuth2()
		//{
		//	dynamic ssoHostResponse = null;
		//	try
		//	{
		//		ssoHostResponse = await URL_HOSTNAME
		//						.WithHeader("User-Agent", USERAGENT)
		//						.WithCookies(out _jar)
		//						.GetJsonAsync<dynamic>();
		//	}
		//	catch (FlurlHttpException e)
		//	{
		//		Log.Error(e, "Failed to authenticate with Garmin. Invalid initial SO request.");
		//		throw;
		//	}

		//	var ssoHostName = ssoHostResponse.host;

		//	object queryParams = new
		//	{
		//		clientId = "GarminConnect",
		//		//connectLegalTerms = "true",
		//		consumeServiceTicket = "false",
		//		createAccountShown = "true",
		//		//cssUrl = "https://connect.garmin.com/gauth-custom-v1.2-min.css",
		//		cssUrl = "https://static.garmincdn.com/com.garmin.connect/ui/css/gauth-custom-v1.2-min.css",
		//		displayNameShown = "false",
		//		embedWidget = "false",
		//		gauthHost = "https://sso.garmin.com/sso",
		//		//generateExtraServiceTicket = "true",
		//		generateExtraServiceTicket = "false",
		//		//generateNoServiceTicket = "false",
		//		//generateTwoExtraServiceTickets = "true",
		//		//globalOptInChecked = "false",
		//		//globalOptInShown = "true",
		//		id = "gauth-widget",
		//		initialFocus = "true",
		//		//locale = "fr_FR",
		//		locale = "en_US",
		//		//locationPromptShown = "true",
		//		//mfaRequired = "false",
		//		//performMFACheck = "false",
		//		//mobile = "false",
		//		openCreateAccount = "false",
		//		//privacyStatementUrl = "https://www.garmin.com/fr-FR/privacy/connect/",
		//		//redirectAfterAccountCreationUrl = "https://connect.garmin.com/modern/",
		//		//redirectAfterAccountLoginUrl = "https://connect.garmin.com/modern/",
		//		redirectAfterAccountCreationUrl = "https://connect.garmin.com/",
		//		redirectAfterAccountLoginUrl = "https://connect.garmin.com/",
		//		rememberMeChecked = "false",
		//		rememberMeShown = "true",
		//		//rememberMyBrowserChecked = "false",
		//		//rememberMyBrowserShown = "false",
		//		//service = "https://connect.garmin.com/modern/",
		//		service = "https://connect.garmin.com",
		//		//showConnectLegalAge = "false",
		//		//showPassword = "true",
		//		//showPrivacyPolicy = "false",
		//		//showTermsOfUse = "false",
		//		//source = "https://connect.garmin.com/signin/",
		//		source = "https://connect.garmin.com",
		//		//useCustomHeader = "false",
		//		usernameShow = "false",
		//		//webhost = ssoHostName.ToString()
		//		//webhost = "https://connect.garmin.com/modern/"
		//		webhost = "https://connect.garmin.com"
		//	};

		//	string loginForm = null;
		//	try
		//	{
		//		loginForm = await URL_LOGIN
		//					.WithHeader("User-Agent", USERAGENT)
		//					.SetQueryParams(queryParams)
		//					.WithCookies(_jar)
		//					.GetStringAsync();

		//	}
		//	catch (FlurlHttpException e)
		//	{
		//		Log.Error(e, "No login form.");
		//		throw;
		//	}

		//	// Lookup CSRF token
		//	var regex = new Regex("<input type=\\\"hidden\\\" name=\\\"_csrf\\\" value=\\\"(\\w+)\\\" />");
		//	var csrfTokenMatch = regex.Match(loginForm);

		//	if (!csrfTokenMatch.Success)
		//	{
		//		Log.Error("No CSRF token.");
		//		throw new Exception("Failed to find CSRF token from Garmin.");
		//	}

		//	var csrfToken = csrfTokenMatch.Groups[1].Value;

		//	object loginData = new
		//	{
		//		embed = "false",
		//		username = _config.Garmin.Email,
		//		password = _config.Garmin.Password,
		//		_csrf = csrfToken
		//	};

		//	string authResponse = null;

		//	try
		//	{
		//		authResponse = await URL_LOGIN
		//						.WithHeader("Host", URL_HOST_SSO)
		//						.WithHeader("Referer", URL_SSO_SIGNIN)
		//						.WithHeader("User-Agent", USERAGENT)
		//						.SetQueryParams(queryParams)
		//						.WithCookies(_jar)
		//						.PostUrlEncodedAsync(loginData)
		//						.ReceiveString();
		//	}
		//	catch (FlurlHttpException e)
		//	{
		//		Log.Error(e, "Authentication Failed.");
		//		throw;
		//	}

		//	// Check we have SSO guid in the cookies
		//	if (!_jar.Any(c => c.Name == "GARMIN-SSO-GUID"))
		//	{
		//		Log.Error("Missing Garmin auth cookie.");
		//		throw new Exception("Failed to find Garmin auth cookie.");
		//	}

		//	// Try to find the full post login url in response
		//	var regex2 = new Regex("var response_url(\\s+) = (\\\"|\\').*?ticket=(?<ticket>[\\w\\-]+)(\\\"|\\')");
		//	var match = regex2.Match(authResponse);
		//	if (!match.Success)
		//	{
		//		Log.Error("Missing service ticket.");
		//		throw new Exception("Failed to find service ticket.");
		//	}

		//	var ticket = match.Groups.GetValueOrDefault("ticket").Value;
		//	if (string.IsNullOrEmpty(ticket))
		//	{
		//		Log.Error("Failed to parse service ticket.");
		//		throw new Exception("Failed to parse service ticket.");
		//	}

		//	queryParams = new
		//	{
		//		ticket = ticket
		//	};

		//	// Second Auth Step
		//	// Needs a service ticket from the previous step
		//	try
		//	{
		//		var authResponse2 = URL_POST_LOGIN
		//					.WithHeader("User-Agent", USERAGENT)
		//					.WithHeader("Host", URL_HOST_CONNECT)
		//					.SetQueryParams(queryParams)
		//					.WithCookies(_jar)
		//					.GetStringAsync();
		//	}
		//	catch (FlurlHttpException e)
		//	{
		//		Log.Error(e, "Second auth step failed.");
		//		throw;
		//	}

		//	// Check login
		//	try
		//	{
		//		var response = PROFILE_URL
		//					.WithHeader("User-Agent", USERAGENT)
		//					.WithCookies(_jar)
		//					.GetJsonAsync();
		//	}
		//	catch (FlurlHttpException e)
		//	{
		//		Log.Error(e, "Login check failed.");
		//		throw;
		//	}
		//}

		// TODO: I bet we can do multiple files at once
		// https://github.com/tmenier/Flurl/issues/608

		/// <summary>
		/// Not quite working. Only uploads the first activity added.
		/// </summary>
		//public async Task<string> UploadActivities(ICollection<string> filePaths, string format)
		//{
		//	var auth = await InitAuth();

		//	var response = await $"{UPLOAD_URL}/{format}"
		//		.WithCookies(auth.CookieJar)
		//		.WithHeader("NK", "NT")
		//		.WithHeader("origin", ORIGIN)
		//		.WithHeader("User-Agent", auth.UserAgent)
		//		.AllowHttpStatus("2xx,409")
		//		.PostMultipartAsync((data) =>
		//		{
		//			foreach (var path in filePaths)
		//			{
		//				var fileName = Path.GetFileName(path);
		//				data.AddFile("\"file\"", path: path, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");
		//			}
		//		})
		//		.ReceiveJson<UploadResponse>();

		//	var result = response.DetailedImportResult;

		//	if (result.Failures.Any())
		//	{
		//		foreach (var failure in result.Failures)
		//		{
		//			if (failure.Messages.Any())
		//			{
		//				foreach (var message in failure.Messages)
		//				{
		//					if (message.Code == 202)
		//					{
		//						_logger.Information("Activity already uploaded {garminWorkout}", result.FileName);
		//					}
		//					else
		//					{
		//						_logger.Error("Failed to upload activity to Garmin. Message: {errorMessage}", message);
		//					}
		//				}
		//			}
		//		}
		//	}

		//	return string.Empty;
		//}

		//public async Task GetDeviceList()
		//{
		//	var auth = await InitAuth();

		//	var response = await $"https://connect.garmin.com/proxy/device-service/deviceregistration/devices"
		//		.WithCookies(auth.CookieJar)
		//		.WithHeader("User-Agent", auth.UserAgent)
		//		.WithHeader("origin", "https://sso.garmin.com")
		//		.GetJsonAsync();
		//}
	}
}
