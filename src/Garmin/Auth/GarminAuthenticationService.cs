using Common.Observe;
using Common.Service;
using Common.Stateful;
using Flurl.Http;
using Serilog;
using System;
using System.Collections.Generic;
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
		gauthHost = "https://sso.garmin.com/sso/embed",
		redirectAfterAccountCreationUrl = "https://sso.garmin.com/sso/embed",
		redirectAfterAccountLoginUrl = "https://sso.garmin.com/sso/embed",
		service = "https://sso.garmin.com/sso/embed",
		source = "https://sso.garmin.com/sso/embed",
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
		/////////////////////////////////////////////////////////////////////////////
		// TODO: Implement refresh using OAuth tokens instead of re-using credentials
		// Eventually remove need to store credentials locally
		///////////////////////////////////////////////////////////////////////////////

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
			csrfToken = FindCsrfToken(tokenResult.RawResponseBody, failureStepCode: Code.FailedPriorToCredentialsUsed);
		}
		catch (FlurlHttpException e)
		{
			throw new GarminAuthenticationError("Failed to fetch csrf token from Garmin.", e) { Code = Code.FailedPriorToCredentialsUsed };
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

			var mfaCsrfToken = FindCsrfToken(sendCredentialsResult.RawResponseBody, failureStepCode: Code.FailedPriorToMfaUsed);
			auth.AuthStage = AuthStage.NeedMfaToken;
			auth.MFACsrfToken = mfaCsrfToken;
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
		var consumerCredentials = await _apiClient.GetConsumerCredentialsAsync();
		await GetOAuth1Async(ticket, auth, consumerCredentials);

		////////////////////////////////////////////
		// Exchange for OAuth2 Token
		///////////////////////////////////////////
		try
		{
			auth.OAuth2Token = await _apiClient.GetOAuth2TokenAsync(auth, consumerCredentials);			
		}
		catch (Exception e)
		{
			throw new GarminAuthenticationError("Auth appeared successful but failed to get the OAuth2 token.", e) { Code = Code.AuthAppearedSuccessful };
		}

		auth.AuthStage = AuthStage.Completed;
		auth.MFACsrfToken = string.Empty;
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
			new KeyValuePair<string, string>("embed", "true"),
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

	private string FindCsrfToken(string rawResponseBody, Code failureStepCode)
	{
		try
		{
			var tokenRegex = new Regex("name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"");
			var match = tokenRegex.Match(rawResponseBody);
			if (!match.Success)
				throw new GarminAuthenticationError($"Failed to find regex match for csrf token. tokenResult: {rawResponseBody}") { Code = failureStepCode };

			var csrfToken = match.Groups.GetValueOrDefault("csrf")?.Value;
			_logger.Verbose($"Csrf Token: {csrfToken}");

			if (string.IsNullOrWhiteSpace(csrfToken))
				throw new GarminAuthenticationError("Found csrfToken but its null.") { Code = failureStepCode };

			return csrfToken;
		} catch (Exception e)
		{
			throw new GarminAuthenticationError("Failed to parse csrf token.", e) { Code = failureStepCode };
		}
	}

	private async Task GetOAuth1Async(string ticket, GarminApiAuthentication auth, ConsumerCredentials credentials)
	{
		string oauth1Response = null;
		try
		{
			oauth1Response = await _apiClient.GetOAuth1TokenAsync(auth, credentials, ticket);
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

		auth.OAuth1Token = new OAuth1Token()
		{
			Token = oAuthToken,
			TokenSecret = oAuthTokenSecret
		};
	}
}
