﻿using Common;
using Flurl.Http;
using Garmin.Dto;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Garmin
{
	public class ApiClient
	{
		private const string BASE_URL = "https://connect.garmin.com";
		private const string SSO_URL = "https://sso.garmin.com";
		private const string SIGNIN_URL = "https://sso.garmin.com/sso/signin";

		private static string PROFILE_URL = $"{BASE_URL}/modern/currentuser-service/user/info";
		private static string UPLOAD_URL = $"{BASE_URL}/modern/proxy/upload-service/upload";

		private const string ORIGIN = SSO_URL;
		private static string USERAGENT = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:48.0) Gecko/20100101 Firefox/50.0";

		private readonly IAppConfiguration _config;

		private CookieJar _jar;

		public ApiClient(IAppConfiguration config)
		{
			_config = config;

			if (!string.IsNullOrEmpty(_config.Developer.UserAgent))
				USERAGENT = _config.Developer.UserAgent;
		}

		/// <summary>
		/// Initialize authentication.
		/// https://github.com/cyberjunky/python-garminconnect/blob/master/garminconnect/__init__.py#L16
		/// </summary>
		public async Task InitAuth()
		{
			object queryParams = new
			{
				clientId = "GarminConnect",
				consumeServiceTicket = "false",
				createAccountShown = "true",
				cssUrl = "https://static.garmincdn.com/com.garmin.connect/ui/css/gauth-custom-v1.2-min.css",
				displayNameShown = "false",
				embedWidget = "false",
				gauthHost = "https://sso.garmin.com/sso",
				generateExtraServiceTicket = "false",
				id = "gauth-widget",
				initialFocus = "true",
				locale = "en_US",
				openCreateAccount = "false",
				redirectAfterAccountCreationUrl = "https://connect.garmin.com/",
				redirectAfterAccountLoginUrl = "https://connect.garmin.com/",
				rememberMeChecked = "false",
				rememberMeShown = "true",
				service = "https://connect.garmin.com",
				source = "https://connect.garmin.com",
				usernameShow = "false",
				webhost = "https://connect.garmin.com"
			};

			string loginForm = null;
			try
			{
				loginForm = await SIGNIN_URL
							.WithHeader("User-Agent", USERAGENT)
							.WithHeader("origin", ORIGIN)
							.SetQueryParams(queryParams)
							.WithCookies(out _jar)
							.GetStringAsync();
			}
			catch (FlurlHttpException e)
			{
				Log.Error(e, "No login form.");
				throw;
			}

			object loginData = new
			{
				embed = "true",
				username = _config.Garmin.Email,
				password = _config.Garmin.Password,
				lt = "e1s1",
				_eventId = "submit",
				displayNameRequired = "false",
			};

			string authResponse = null;
			try
			{
				authResponse = await SIGNIN_URL
								.WithHeader("User-Agent", USERAGENT)
								.WithHeader("origin", ORIGIN)
								.SetQueryParams(queryParams)
								.WithCookies(_jar)
								.PostUrlEncodedAsync(loginData)
								.ReceiveString();
			}
			catch (FlurlHttpException e)
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

			//Try to find the full post login url in response
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

			queryParams = new
			{
				ticket = ticket
			};

			// Second Auth Step
			// Needs a service ticket from the previous step
			try
			{
				var authResponse2 = await BASE_URL
							.WithCookies(_jar)
							.SetQueryParams(queryParams)
							.GetStringAsync();
			}
			catch (FlurlHttpException e)
			{
				Log.Error(e, "Second auth step failed.");
				throw;
			}

			// Check login
			try
			{
				var response = await PROFILE_URL
							.WithHeader("User-Agent", USERAGENT)
							.WithHeader("origin", ORIGIN)
							.WithCookies(_jar)
							.GetJsonAsync();
			}
			catch (FlurlHttpException e)
			{
				Log.Error(e, "Login check failed.");
				throw;
			}
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
		
		public async Task<UploadResponse> UploadActivity(string filePath, string format)
		{
			var fileName = Path.GetFileName(filePath);
			var response = await $"{UPLOAD_URL}/{format}"
				.WithCookies(_jar)
				.WithHeader("NK", "NT")
				.WithHeader("origin", ORIGIN)
				.WithHeader("User-Agent", USERAGENT)
				.AllowHttpStatus("2xx,409")
				.PostMultipartAsync((data) => 
				{
					data.AddFile("\"file\"", path: filePath, contentType: "application/octet-stream", fileName:$"\"{fileName}\"");
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
								Log.Information("Activity already uploaded {garminWorkout}", result.FileName);
							} else
							{
								Log.Error("Failed to upload activity to Garmin. Message: {errorMessage}", message);
							}
						}
					}
				}
			}

			return response;
		}

		/// <summary>
		/// Not quite working. Only uploads the first activity added.
		/// </summary>
		public async Task<string> UploadActivities(ICollection<string> filePaths, string format)
		{
			
			var response = await $"{UPLOAD_URL}/{format}"
				.WithCookies(_jar)
				.WithHeader("NK", "NT")
				.WithHeader("origin", ORIGIN)
				.WithHeader("User-Agent", USERAGENT)
				.AllowHttpStatus("2xx,409")
				.PostMultipartAsync((data) =>
				{
					foreach (var path in filePaths)
					{
						var fileName = Path.GetFileName(path);
						data.AddFile("\"file\"", path: path, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");
					}
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
								Log.Information("Activity already uploaded {garminWorkout}", result.FileName);
							}
							else
							{
								Log.Error("Failed to upload activity to Garmin. Message: {errorMessage}", message);
							}
						}
					}
				}
			}

			return string.Empty;
		}

		public async Task GetDeviceList()
		{
			var response = await $"https://connect.garmin.com/proxy/device-service/deviceregistration/devices"
				.WithCookies(_jar)
				.WithHeader("User-Agent", USERAGENT)
				.WithHeader("origin", "https://sso.garmin.com")
				.GetJsonAsync();
		}
	}
}
