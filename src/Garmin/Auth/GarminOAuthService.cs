using Common.Service;
using Common.Stateful;
using Flurl.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OAuth;
using System.Collections.Generic;
using Common.Observe;
using Serilog;
using System.Web;

namespace Garmin.Auth;
public interface IGarminOAuthService
{
}

public class GarminOAuthService : IGarminOAuthService
{
	private static readonly ILogger _logger = LogContext.ForClass<GarminOAuthService>();

	private readonly ISettingsService _settingsService;
	private readonly IGarminApiClient _apiClient;

	public GarminOAuthService(ISettingsService settingsService, IGarminApiClient apiClient)
	{
		_settingsService = settingsService;
		_apiClient = apiClient;
	}

	public async Task GetAuthTokenAsync()
	{
		var auth = new GarminApiAuthentication();
		//auth.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";
		auth.UserAgent = "com.garmin.android.apps.connectmobile";
		auth.Email = "";
		auth.Password = "";

		/////////////////////////////////
		// Init Cookie Jar
		////////////////////////////////
		var queryParams = new
		{
			id = "gauth-widget",
			embedWidget = "true",
			gauthHost = "https://sso.garmin.com/sso"
		};

		CookieJar jar = null;
		try
		{
			await _apiClient.InitCookieJarAsync(queryParams, auth.UserAgent, out jar);
		}
		catch (FlurlHttpException e)
		{
			throw new GarminAuthenticationError("Failed to initialize sign in flow.", e) { Code = Code.FailedPriorToCredentialsUsed };
		}

		/////////////////////////////////
		// Get CSRF token
		////////////////////////////////
		object csrfRequest = new
		{
			id = "gauth-widget",
			embedWidget = "true",
			gauthHost = "https://sso.garmin.com/sso/embed",
			service = "https://sso.garmin.com/sso/embed",
			source = "https://sso.garmin.com/sso/embed",
			redirectAfterAccountLoginUrl = "https://sso.garmin.com/sso/embed",
			redirectAfterAccountCreationUrl = "https://sso.garmin.com/sso/embed",
		};

		var tokenResult = await _apiClient.GetCsrfTokenAsync(auth, csrfRequest, jar);
		var tokenRegex = new Regex("name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"");
		var match = tokenRegex.Match(tokenResult.RawResponseBody);
		if (!match.Success)
			throw new Exception("Failed to regex match token");

		var csrfToken = match.Groups.GetValueOrDefault("csrf")?.Value;
		_logger.Verbose($"Csrf Token: {csrfToken}");

		/////////////////////////////////
		// Submit login form
		////////////////////////////////
		var loginData = new
		{
			username = auth.Email,
			password = auth.Password,
			embed = "true",
			_csrf = csrfToken
		};
		var signInResult = await _apiClient.SendCredentialsAsync(auth, csrfRequest, loginData, jar);

		if (signInResult.WasRedirected && signInResult.RedirectedTo.Contains("https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"))
		{
			// todo: handle mfa flow
			throw new NotImplementedException("handle mfa");
		}

		var ticketRegex = new Regex("embed\\?ticket=(?<ticket>[^\"]+)\"");
		var ticketMatch = ticketRegex.Match(signInResult.RawResponseBody);
		if (!ticketMatch.Success)
			throw new Exception("Filed to find post signin ticket.");

		var ticket = ticketMatch.Groups.GetValueOrDefault("ticket").Value;
		_logger.Verbose($"Service Ticket: {ticket}");

		/////////////////////////////////
		// Get OAuth Tokens
		////////////////////////////////
		var (oAuthToken, oAuthTokenSecret) = await GetOAuth1Async(ticket, auth.UserAgent);

		/////////////////////////////////
		// Exchane for OAuth2
		////////////////////////////////
		var oAuth2Token = await GetOAuth2TokenAsync(oAuthToken, oAuthTokenSecret, auth.UserAgent);

		/////////////////////////////////
		// Test
		////////////////////////////////
		await "https://connect.garmin.com/weight-service/weight/range/2023-08-15/2023-09-26"
			.WithOAuthBearerToken(oAuth2Token.Access_Token)
			.GetAsync();
	}

	private async Task<(string oAuthToken, string oAuthTokenSecret)> GetOAuth1Async(string ticket, string userAgent)
	{
		// todo: don't hard code
		var consumerKey = "fc3e99d2-118c-44b8-8ae3-03370dde24c0";
		var consumerSecret = "E08WAR897WEy2knn7aFBrvegVAf0AFdWBBF";

		OAuthRequest oauthClient = OAuthRequest.ForRequestToken(consumerKey, consumerSecret);
		oauthClient.RequestUrl = $"https://connectapi.garmin.com/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true";

		var result = await oauthClient.RequestUrl
							.WithHeader("User-Agent", userAgent)
							.WithHeader("Authorization", oauthClient.GetAuthorizationHeader())
							.GetStringAsync();

		var queryParams = HttpUtility.ParseQueryString(result);

		if (queryParams.Count < 2)
			throw new Exception($"Result length did not match expected: {result.Length}");

		var oAuthToken = queryParams.Get("oauth_token");
		var oAuthTokenSecret = queryParams.Get("oauth_token_secret");

		if (string.IsNullOrWhiteSpace(oAuthToken))
			throw new Exception("OAuth1 token is null");

		if (string.IsNullOrWhiteSpace(oAuthTokenSecret))
			throw new Exception("OAuth1 token secret is null");

		return (oAuthToken, oAuthTokenSecret);
	}

	private async Task<OAuth2Token> GetOAuth2TokenAsync(string oAuthToken, string oAuthTokenSecret, string userAgent)
	{
		// todo: don't hard code
		var consumerKey = "fc3e99d2-118c-44b8-8ae3-03370dde24c0";
		var consumerSecret = "E08WAR897WEy2knn7aFBrvegVAf0AFdWBBF";

		OAuthRequest oauthClient2 = OAuthRequest.ForProtectedResource("POST", consumerKey, consumerSecret, oAuthToken, oAuthTokenSecret);
		oauthClient2.RequestUrl = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";

		var token = await oauthClient2.RequestUrl
							.WithHeader("User-Agent", userAgent)
							.WithHeader("Authorization", oauthClient2.GetAuthorizationHeader())
							.WithHeader("Content-Type", "application/x-www-form-urlencoded")
							.PostAsync()
							.ReceiveJson<OAuth2Token>();

		return token;
	}
}
