using Common.Observe;
using Common.Service;
using Common.Stateful;
using Flurl.Http;
using OAuth;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Garmin.Auth;

public interface IGarminAuthenticationService
{
	Task<GarminApiAuthentication> GetGarminAuthenticationAsync();
	Task<GarminApiAuthentication> RefreshGarminAuthenticationAsync();
	Task<GarminApiAuthentication> CompleteMFAAuthAsync(string mfaCode);

}

public class GarminAuthenticationService : IGarminAuthenticationService
{
	private static readonly ILogger _logger = LogContext.ForClass<GarminAuthenticationService>();
	private static readonly object CommonQueryParams = new
	{
		id = "gauth-widget",
		embedWidget = "true",
		gauthHost = "https://sso.garmin.com/sso"
	};

	private readonly ISettingsService _settingsService;
	private readonly IGarminApiClient _apiClient;

	public GarminAuthenticationService(ISettingsService settingsService, IGarminApiClient apiClient)
	{
		_settingsService = settingsService;
		_apiClient = apiClient;
	}

	public async Task<GarminApiAuthentication> GetGarminAuthenticationAsync()
	{
		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin.EnsureGarminCredentialsAreProvided();

		var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);
		if (auth is object && auth.IsValid(settings))
			return auth;

		return await RefreshGarminAuthenticationAsync();
	}

	public async Task<GarminApiAuthentication> RefreshGarminAuthenticationAsync()
	{
		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin.EnsureGarminCredentialsAreProvided();

		_settingsService.ClearGarminAuthentication(settings.Garmin.Email);

		var auth = new GarminApiAuthentication();
		auth.Email = settings.Garmin.Email;
		auth.Password = settings.Garmin.Password;
		CookieJar jar = null;
		auth.AuthStage = AuthStage.None;

		var appConfig = await _settingsService.GetAppConfigurationAsync();
		if (!string.IsNullOrEmpty(appConfig.Developer.UserAgent))
			auth.UserAgent = appConfig.Developer.UserAgent;

		/////////////////////////////////
		// Init Auth Flow
		////////////////////////////////
		try
		{
			await _apiClient.InitCookieJarAsync(CommonQueryParams, auth.UserAgent, out jar);
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

		string csrfToken = string.Empty;
		try
		{
			var tokenResult = await _apiClient.GetCsrfTokenAsync(auth, csrfRequest, jar);

			var tokenRegex = new Regex("name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"");
			var match = tokenRegex.Match(tokenResult.RawResponseBody);
			if (!match.Success)
				throw new GarminAuthenticationError($"Failed to find regex match for csrf token. tokenResult: {tokenResult}") { Code = Code.FailedPriorToCredentialsUsed };

			csrfToken = match.Groups.GetValueOrDefault("csrf")?.Value;
			_logger.Verbose($"Csrf Token: {csrfToken}");

			if (string.IsNullOrWhiteSpace(csrfToken))
				throw new GarminAuthenticationError("Found csrfToken but its null.") { Code = Code.FailedPriorToCredentialsUsed };
		}
		catch (FlurlHttpException e)
		{
			throw new GarminAuthenticationError("Failed to fetch csrf token from Garmin.", e) { Code = Code.FailedPriorToCredentialsUsed };
		}
		catch (Exception e)
		{
			throw new GarminAuthenticationError("Failed to parse csrf token.", e) { Code = Code.FailedPriorToCredentialsUsed };
		}

		/////////////////////////////////
		// Send Credentials
		////////////////////////////////
		var sendCredentialsRequest = new
		{
			username = auth.Email,
			password = auth.Password,
			embed = "true",
			_csrf = csrfToken
		};
		SendCredentialsResult sendCredentialsResult = null;
		try
		{
			sendCredentialsResult = await _apiClient.SendCredentialsAsync(auth, csrfRequest, sendCredentialsRequest, jar);
		}
		catch (FlurlHttpException e) when (e.StatusCode is (int)HttpStatusCode.Forbidden)
		{
			var responseContent = (await e.GetResponseStringAsync()) ?? string.Empty;

			if (responseContent == "error code: 1020")
				throw new GarminAuthenticationError("Garmin Authentication Failed. Blocked by CloudFlare.", e) { Code = Code.Cloudflare };

			throw new GarminAuthenticationError("Garmin Authentication Failed.", e) { Code = Code.InvalidCredentials };
		}

		/////////////////////////////////
		// Conditionally Handle MFA
		////////////////////////////////
		if (sendCredentialsResult.WasRedirected && sendCredentialsResult.RedirectedTo.Contains("https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"))
		{
			if (!settings.Garmin.TwoStepVerificationEnabled)
				throw new GarminAuthenticationError("Detected Garmin TwoFactorAuthentication but TwoFactorAuthenctication is not enabled in P2G settings. Please enable TwoFactorAuthentication in your P2G Garmin settings.") { Code = Code.UnexpectedMfa };

			SetMFACsrfToken(auth, sendCredentialsResult.RawResponseBody);
			auth.CookieJar = jar;
			_settingsService.SetGarminAuthentication(auth);
			return auth;
		}

		var loginResult = sendCredentialsResult?.RawResponseBody;
		return await CompleteGarminAuthenticationAsync(loginResult, auth);
	}

	private async Task<GarminApiAuthentication> CompleteGarminAuthenticationAsync(string loginResult, GarminApiAuthentication auth)
	{
		// Try to find the full post login ServiceTicket
		var ticketRegex = new Regex("embed\\?ticket=(?<ticket>[^\"]+)\"");
		var ticketMatch = ticketRegex.Match(loginResult);
		if (!ticketMatch.Success)
			throw new GarminAuthenticationError("Auth appeared successful but failed to find regex match for service ticket.") { Code = Code.AuthAppearedSuccessful };

		var ticket = ticketMatch.Groups.GetValueOrDefault("ticket").Value;
		_logger.Verbose($"Service Ticket: {ticket}");

		if (string.IsNullOrWhiteSpace(ticket))
			throw new GarminAuthenticationError("Auth appeared successful, and found service ticket, but ticket was null or empty.") { Code = Code.AuthAppearedSuccessful };

		////////////////////////////////////////////
		// Get OAuth1 Tokens
		///////////////////////////////////////////
		var (oAuthToken, oAuthTokenSecret) = await GetOAuth1Async(ticket, auth.UserAgent);

		////////////////////////////////////////////
		// Exchange for OAuth2 Token
		///////////////////////////////////////////
		var oAuth2Token = await GetOAuth2TokenAsync(oAuthToken, oAuthTokenSecret, auth.UserAgent);

		auth.AuthStage = AuthStage.Completed;
		auth.OAuth2Token = oAuth2Token;
		_settingsService.SetGarminAuthentication(auth);
		return auth;
	}

	public async Task<GarminApiAuthentication> CompleteMFAAuthAsync(string mfaCode)
	{
		var settings = await _settingsService.GetSettingsAsync();
		var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);

		if (auth is null || auth.AuthStage == AuthStage.None)
			throw new ArgumentException("Garmin Auth has not been initialized, cannot provide MFA token yet.");

		if (auth.AuthStage != AuthStage.NeedMfaToken)
			return auth;

		var mfaData = new List<KeyValuePair<string, string>>()
		{
			new KeyValuePair<string, string>("embed", "false"),
			new KeyValuePair<string, string>("mfa-code", mfaCode),
			new KeyValuePair<string, string>("fromPage", "setupEnterMfaCode"),
			new KeyValuePair<string, string>("_csrf", auth.MFACsrfToken)
		};

		/////////////////////////////////
		// Send the MFA Code to Garmin
		////////////////////////////////
		try
		{
			SendMFAResult mfaResponse = new();
			mfaResponse.RawResponseBody = await _apiClient.SendMfaCodeAsync(auth.UserAgent, CommonQueryParams, mfaData, auth.CookieJar);
			return await CompleteGarminAuthenticationAsync(mfaResponse.RawResponseBody, auth);
		}
		catch (FlurlHttpException e) when (e.StatusCode is (int)HttpStatusCode.Forbidden)
		{
			var responseContent = (await e.GetResponseStringAsync()) ?? string.Empty;

			if (responseContent == "error code: 1020")
				throw new GarminAuthenticationError("MFA: Garmin Authentication Failed. Blocked by CloudFlare.", e) { Code = Code.Cloudflare };

			throw new GarminAuthenticationError("MFA: MFA Code rejected by Garmin.", e) { Code = Code.InvalidMfaCode };
		}
	}

	private void SetMFACsrfToken(GarminApiAuthentication auth, string sendCredentialsResponseBody)
	{
		/////////////////////////////////
		// Try to find the csrf Token
		////////////////////////////////
		var regex3 = new Regex("name=\"_csrf\"\\s+value=\"(?<csrf>[A-Z0-9]+)");
		var match3 = regex3.Match(sendCredentialsResponseBody);
		if (!match3.Success)
			throw new GarminAuthenticationError("MFA: Failed to find csrf token.") { Code = Code.FailedPriorToMfaUsed };

		var csrfToken = match3.Groups.GetValueOrDefault("csrf")?.Value;
		_logger.Verbose($"_csrf Token: {csrfToken}");
		if (string.IsNullOrEmpty(csrfToken))
			throw new GarminAuthenticationError("MFA: Found csrf token but it was null or empty.") { Code = Code.FailedPriorToMfaUsed };

		auth.AuthStage = AuthStage.NeedMfaToken;
		auth.MFACsrfToken = csrfToken;
	}

	private async Task<(string oAuthToken, string oAuthTokenSecret)> GetOAuth1Async(string ticket, string userAgent)
	{
		// todo: don't hard code
		var consumerKey = "fc3e99d2-118c-44b8-8ae3-03370dde24c0";
		var consumerSecret = "E08WAR897WEy2knn7aFBrvegVAf0AFdWBBF";

		OAuthRequest oauthClient = OAuthRequest.ForRequestToken(consumerKey, consumerSecret);
		oauthClient.RequestUrl = $"https://connectapi.garmin.com/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true";

		string oauth1Response = null;
		try
		{
			oauth1Response = await oauthClient.RequestUrl
							.WithHeader("User-Agent", userAgent)
							.WithHeader("Authorization", oauthClient.GetAuthorizationHeader())
							.GetStringAsync();
		} catch (Exception e)
		{
			throw new GarminAuthenticationError("Auth appeared successful but failed to get the OAuth1 token.", e) { Code = Code.AuthAppearedSuccessful };
		}

		if (string.IsNullOrWhiteSpace(oauth1Response))
			throw new GarminAuthenticationError("Auth appeared successful but returned OAuth1 Token response is null.") { Code = Code.AuthAppearedSuccessful };

		var queryParams = HttpUtility.ParseQueryString(oauth1Response);

		var oAuthToken = queryParams.Get("oauth_token");
		var oAuthTokenSecret = queryParams.Get("oauth_token_secret");

		if (string.IsNullOrWhiteSpace(oAuthToken))
			throw new GarminAuthenticationError($"Auth appeared successful but returned OAuth1 token is null. oauth1Response: {oauth1Response}") { Code = Code.AuthAppearedSuccessful };

		if (string.IsNullOrWhiteSpace(oAuthTokenSecret))
			throw new GarminAuthenticationError($"Auth appeared successful but returned OAuth1 token secret is null. oauth1Response: {oauth1Response}") { Code = Code.AuthAppearedSuccessful };

		return (oAuthToken, oAuthTokenSecret);
	}

	private async Task<OAuth2Token> GetOAuth2TokenAsync(string oAuthToken, string oAuthTokenSecret, string userAgent)
	{
		// todo: don't hard code
		var consumerKey = "fc3e99d2-118c-44b8-8ae3-03370dde24c0";
		var consumerSecret = "E08WAR897WEy2knn7aFBrvegVAf0AFdWBBF";

		OAuthRequest oauthClient2 = OAuthRequest.ForProtectedResource("POST", consumerKey, consumerSecret, oAuthToken, oAuthTokenSecret);
		oauthClient2.RequestUrl = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";

		try
		{
			var token = await oauthClient2.RequestUrl
										.WithHeader("User-Agent", userAgent)
										.WithHeader("Authorization", oauthClient2.GetAuthorizationHeader())
										.WithHeader("Content-Type", "application/x-www-form-urlencoded") // this header is required, without it you get a 500
										.PostUrlEncodedAsync(new object()) // hack: PostAsync() will drop the content-type header, by posting empty object we trick flurl into leaving the header
										.ReceiveJson<OAuth2Token>();

			return token;
		} catch (Exception e)
		{
			throw new GarminAuthenticationError("Auth appeared successful but failed to get the OAuth2 token.", e) { Code = Code.AuthAppearedSuccessful };
		}
	}
}
