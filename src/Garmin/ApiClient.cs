using Common;
using Flurl.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		private readonly Configuration _config;

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
			var userAgent = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:48.0) Gecko/20100101 Firefox/50.0";

			CookieJar jar = null;
			dynamic ssoHostResponse = null;
			try
			{
				ssoHostResponse = await URL_HOSTNAME
								.WithHeader("User-Agent", userAgent)
								.SetQueryParams()
								.WithCookies(out jar)
								.GetJsonAsync<dynamic>();
			} catch (FlurlHttpException e)
			{
				Log.Error(e, "Failed to authenticate with Garmin. Invalid initial SO request.");
				throw;
			}

			var ssoHostName = ssoHostResponse.host;

			dynamic queryParams = new 
			{
				clientId = "GarminConnect",
				connectLegalTerms = "true",
				consumeServiceTicket = "false",
				createAccountShown = "true",
				cssUrl = "https://connect.garmin.com/guath-custom-v1.2-min.css",
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
				mobile = "false",
				openCreateAccount = "false",
				privateStatementUrl = "https://www.garmin.com/fr-FR/privacy/connect/",
				redirectAfterAccountCreationUrl = "https://connect.garmin.com/modern/",
				redirectAfterAccountLoginurl = "https://connect.garmin.com/modern/",
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
				webhost = ssoHostName
			};



		}
	}
}
