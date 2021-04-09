using Common;
using Flurl.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Garmin
{
	public class ApiClient
	{
		private const string URL_HOSTNAME = "https://connect.garmin.com/modern/auth/hostname";
		private const string URL_LOGIN = "https://sso.garmin.com/sso/login";
		private const string URL_POST_LOGIN = "https://connect.garmin.com/modern/";
		private const string URL_PROFILE = "https://connect.garmin.com/modern/currentuser-service/user/info";
		private const string URL_HOST_SSO = "sso.garmin.com";
		private const string URL_HOST_CONNECT = "connect.garmin.com";
		private const string URL_SSO_SIGNIN = "https://sso.garmin.com/sso/signin";
		private const string URL_UPLOAD = "https://connect.garmin.com/modern/proxy/upload-service/upload";
		private const string URL_ACTIVITY_BASE = "https://connect.garmin.com/modern/proxy/activity-service/activity";
		private const string URL_ACTIVITY_TYPES = "https://connect.garmin.com/modern/proxy/activity-service/activity/activityTypes";
		private const string USERAGENT = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:48.0) Gecko/20100101 Firefox/50.0";

		private readonly Configuration _config;

		private CookieJar _jar;

		public ApiClient(Configuration config)
		{
			_config = config;
		}

		/// <summary>
		/// This is where the magic happens!
		/// Straight from  https://github.com/La0/garmin-uploader
		/// </summary>
		public async Task InitAuth()
		{
			dynamic ssoHostResponse = null;
			try
			{
				ssoHostResponse = await URL_HOSTNAME
								.WithHeader("User-Agent", USERAGENT)
								.WithCookies(out _jar)
								.GetJsonAsync<dynamic>();
			} catch (FlurlHttpException e)
			{
				Log.Error(e, "Failed to authenticate with Garmin. Invalid initial SO request.");
				throw;
			}

			var ssoHostName = ssoHostResponse.host;

			object queryParams = new
			{
				clientId = "GarminConnect",
				connectLegalTerms = "true",
				consumeServiceTicket = "false",
				createAccountShown = "true",
				cssUrl = "https://connect.garmin.com/gauth-custom-v1.2-min.css",
				displayNameShown = "false",
				embedWidget = "false",
				gauthHost = "https://sso.garmin.com/sso",
				generateExtraServiceTicket = "true",
				generateNoServiceTicket = "false",
				generateTwoExtraServiceTickets = "true",
				globalOptInChecked = "false",
				globalOptInShown = "true",
				id = "gauth-widget",
				initialFocus = "true",
				locale = "fr_FR",
				locationPromptShown = "true",
				mfaRequired = "false",
				performMFACheck = "false",
				mobile = "false",
				openCreateAccount = "false",
				privacyStatementUrl = "https://www.garmin.com/fr-FR/privacy/connect/",
				redirectAfterAccountCreationUrl = "https://connect.garmin.com/modern/",
				redirectAfterAccountLoginUrl = "https://connect.garmin.com/modern/",
				rememberMeChecked = "false",
				rememberMeShown = "true",
				rememberMyBrowserChecked = "false",
				rememberMyBrowserShown = "false",
				service = "https://connect.garmin.com/modern/",
				showConnectLegalAge = "false",
				showPassword = "true",
				showPrivacyPolicy = "false",
				showTermsOfUse = "false",
				source = "https://connect.garmin.com/signin/",
				useCustomHeader = "false",
				//webhost = ssoHostName.ToString()
				webhost = "https://connect.garmin.com/modern/"
			};

			string loginForm = null;
			try
			{
				loginForm = await URL_LOGIN
							.WithHeader("User-Agent", USERAGENT)
							.SetQueryParams(queryParams)
							.WithCookies(_jar)
							.GetStringAsync();
			} catch (FlurlHttpException e)
			{
				Log.Error(e, "No login form.");
				throw;
			}

			// Lookup CSRF token
			var regex = new Regex("<input type=\\\"hidden\\\" name=\\\"_csrf\\\" value=\\\"(\\w+)\\\" />");
			var csrfTokenMatch = regex.Match(loginForm);

			if (!csrfTokenMatch.Success)
			{
				Log.Error("No CSRF token.");
				throw new Exception("Failed to find CSRF token from Garmin.");
			}

			var csrfToken = csrfTokenMatch.Groups[1].Value;

			object loginData = new
			{
				embed = "false",
				username = _config.Garmin.Email,
				password = _config.Garmin.Password,
				_csrf = csrfToken
			};

			string authResponse = null;

			try
			{
				authResponse = await URL_LOGIN
								.WithHeader("Host", URL_HOST_SSO)
								.WithHeader("Referer", URL_SSO_SIGNIN)
								.WithHeader("User-Agent", USERAGENT)
								.SetQueryParams(queryParams)
								.WithCookies(_jar)
								.PostUrlEncodedAsync(loginData)
								.ReceiveString();
			} catch (FlurlHttpException e)
			{
				Log.Error(e, "Authentication Failed.");
				throw;
			}

			// Check we have SSO guid in the cookies
			if (!_jar.Any(c => c.Name == "GARMIN-SSO-GUID"))
			{
				Log.Error("Missing Garmin auth cookie.");
				throw new Exception("Failed to find Garmin auth cookie.");
			}

			// Try to find the full post login url in response
			var regex2 = new Regex("var response_url(\\s+) = (\\\"|\\').*?ticket=(?<ticket>[\\w\\-]+)(\\\"|\\')");
			var match = regex2.Match(authResponse);
			if (!match.Success)
			{
				Log.Error("Missing service ticket.");
				throw new Exception("Failed to find service ticket.");
			}

			var ticket = match.Groups.GetValueOrDefault("ticket").Value;
			if (string.IsNullOrEmpty(ticket))
			{
				Log.Error("Failed to parse service ticket.");
				throw new Exception("Failed to parse service ticket.");
			}

			queryParams = new {
				ticket = ticket
			};

			// Second Auth Step
			// Needs a service ticket from the previous step
			try
			{
				var authResponse2 = URL_POST_LOGIN
							.WithHeader("User-Agent", USERAGENT)
							.WithHeader("Host", URL_HOST_CONNECT)
							.SetQueryParams(queryParams)
							.WithCookies(_jar)
							.GetStringAsync();
			} catch (FlurlHttpException e)
			{
				Log.Error(e, "Second auth step failed.");
				throw;
			}
			
			// Check login
			try
			{
				var response = URL_PROFILE
							.WithHeader("User-Agent", USERAGENT)
							.WithCookies(_jar)
							.GetJsonAsync();
			} catch (FlurlHttpException e)
			{
				Log.Error(e, "Login check failed.");
				throw;
			}
		}

		// TODO: I bet we can do multiple files at once
		public async Task<string> UploadActivity(string name, string filePath, string format)
		{
			var response = await $"{URL_UPLOAD}/{format}"
				.WithCookies(_jar)
				.WithHeader("NK", "NT")
				//.WithHeader("User-Agent", USERAGENT)
				.PostMultipartAsync((data) => 
				{
					data.AddFile("file", filePath, contentType: "application/octet-stream");
				})
				.ReceiveJson();

			var result = response.detailedImportResult;
			if (result.successes.Length == 0)
			{
				if (result.failures.Length > 0)
				{
					if (result.failures[0].messages[0].code == 202)
					{
						Log.Information("Activity already uploaded {garminWorkoutId}", result.failures[0].internalId);
					}
					else
					{
						Log.Error("Failed to upload activity to Garmin. Message: {errorMessage}", result);
					}
				}
			}

			return result.succeses[0].internalId.ToString();
		}
	}
}
