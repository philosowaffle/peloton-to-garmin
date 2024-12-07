using Common.Dto;
using Common.Observe;
using Common.Service;
using Flurl.Http;
using Garmin.Database;
using Garmin.Dto;
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
	Task<bool> GarminAuthTokenExistsAndIsValidAsync();
	Task<GarminApiAuthentication> SignInAsync();
	Task<GarminApiAuthentication> CompleteMFAAuthAsync(string mfaCode);
	Task<bool> SignOutAsync();
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
	private readonly IGarminDb _garminDb;

	public GarminAuthenticationService(ISettingsService settingsService, IGarminApiClient apiClient, IGarminDb garminDb)
	{
		_settingsService = settingsService;
		_apiClient = apiClient;
		_garminDb = garminDb;
	}

	public async Task<bool> GarminAuthTokenExistsAndIsValidAsync()
	{
		var oAuth2Token = await _garminDb.GetGarminOAuth2TokenAsync(1);
		var oAuth1Token = await _garminDb.GetGarminOAuth1TokenAsync(1);

		// we either already have an oAuth2Token, or we think we are capable of getting one without
		// user intervention
		return (oAuth2Token is object && !oAuth2Token.IsExpired()) || (oAuth1Token is object);
	}

	public async Task<GarminApiAuthentication> GetGarminAuthenticationAsync()
	{
		var oAuth2Token = await _garminDb.GetGarminOAuth2TokenAsync(1);
		if (oAuth2Token is object && !oAuth2Token.IsExpired())
			return new GarminApiAuthentication()
			{
				AuthStage = AuthStage.Completed,
				OAuth2Token = oAuth2Token,
			};

		var oAuth1Token = await _garminDb.GetGarminOAuth1TokenAsync(1);
		if (oAuth1Token is object)
		{
			try
			{
				var consumerCredentials = await _apiClient.GetConsumerCredentialsAsync();
				var appConfig = await _settingsService.GetAppConfigurationAsync();

				return await ExchangeOAuth1ForOAuth2Async(oAuth1Token, consumerCredentials);

			} catch (Exception ex)
			{
				_logger.Debug("Failed to exchange OAuth1 token for OAuth 2, will try refreshing OAuth1 token.", ex);
			}
		}

		return await SignInAsync();
	}

	public async Task<GarminApiAuthentication> SignInAsync()
	{
		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin.EnsureGarminCredentialsAreProvided();

		await SignOutAsync();

		CookieJar jar = null;

		var appConfig = await _settingsService.GetAppConfigurationAsync();

		/////////////////////////////////
		// Init Auth Flow
		////////////////////////////////
		try
		{
			jar = await _apiClient.InitCookieJarAsync(CommonQueryParams);
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
			gauthHost = settings.Garmin.Api.SsoEmbedUrl,
			service = settings.Garmin.Api.SsoEmbedUrl,
			source = settings.Garmin.Api.SsoEmbedUrl,
			redirectAfterAccountLoginUrl = settings.Garmin.Api.SsoEmbedUrl,
			redirectAfterAccountCreationUrl = settings.Garmin.Api.SsoEmbedUrl,
		};

		var csrfToken = string.Empty;
		try
		{
			var tokenResult = await _apiClient.GetCsrfTokenAsync(csrfRequest, jar);
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
			username = settings.Garmin.Email,
			password = settings.Garmin.Password,
			embed = "true",
			_csrf = csrfToken
		};
		SendCredentialsResult sendCredentialsResult = null;
		try
		{
			sendCredentialsResult = await _apiClient.SendCredentialsAsync(settings.Garmin.Email, settings.Garmin.Password, csrfRequest, sendCredentialsRequest, jar);
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
				throw new GarminAuthenticationError("Detected Garmin TwoFactorAuthentication but TwoFactorAuthentication is not enabled in P2G settings. Please enable TwoFactorAuthentication in your P2G Garmin settings.") { Code = Code.UnexpectedMfa };

			var mfaCsrfToken = FindCsrfToken(sendCredentialsResult.RawResponseBody, failureStepCode: Code.FailedPriorToMfaUsed);

			var partialAuthentication = new StagedPartialGarminAuthentication()
			{
				ExpiresAt = DateTime.Now.AddMinutes(15),
				AuthStage = AuthStage.NeedMfaToken,
				MFACsrfToken = mfaCsrfToken,
				CookieJarString = jar.ToString(),
			};
			await _garminDb.UpsertPartialGarminAuthenticationAsync(1, partialAuthentication);

			return new GarminApiAuthentication() { AuthStage = AuthStage.NeedMfaToken };
		}

		var loginResult = sendCredentialsResult?.RawResponseBody;
		return await CompleteGarminAuthenticationAsync(loginResult);
	}

	private async Task<GarminApiAuthentication> CompleteGarminAuthenticationAsync(string loginResult)
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
		var oAuth1Token = await GetOAuth1Async(ticket, consumerCredentials);
		await _garminDb.UpsertGarminOAuth1TokenAsync(1, oAuth1Token);

		////////////////////////////////////////////
		// Exchange for OAuth2 Token
		///////////////////////////////////////////
		var result = await ExchangeOAuth1ForOAuth2Async(oAuth1Token, consumerCredentials);

		// Clear partial data
		await _garminDb.UpsertPartialGarminAuthenticationAsync(1, null);

		return result;
	}

	private async Task<GarminApiAuthentication> ExchangeOAuth1ForOAuth2Async(OAuth1Token oAuth1Token, ConsumerCredentials consumerCredentials)
	{
		OAuth2Token oAuth2Token = null;
		try
		{
			oAuth2Token = await _apiClient.GetOAuth2TokenAsync(oAuth1Token, consumerCredentials);
			oAuth2Token.ExpiresAt = DateTime.Now.AddSeconds(oAuth2Token.Expires_In);
			await _garminDb.UpsertGarminOAuth2TokenAsync(1, oAuth2Token);

			//auth.OAuth2Token.Refresh_Token; // not used according to this thread: https://github.com/matin/garth/issues/21
			//auth.OAuth2Token.Expires_In; // ~24hrs
			//auth.OAuth2Token.Refresh_Token_Expires_In; // ~30d
		}
		catch (Exception e)
		{
			throw new GarminAuthenticationError("Auth appeared successful but failed to get the OAuth2 token.  If this persists, try clearing then re-entering your Garmin credentials in the Settings.", e) { Code = Code.AuthAppearedSuccessful };
		}

		return new GarminApiAuthentication()
		{
			AuthStage = AuthStage.Completed,
			OAuth2Token = oAuth2Token
		};
	}

	public async Task<GarminApiAuthentication> CompleteMFAAuthAsync(string mfaCode)
	{
		var partialAuth = await _garminDb.GetStagedPartialGarminAuthenticationAsync(1);

		if (partialAuth is null || partialAuth.AuthStage == AuthStage.None)
			throw new ArgumentException("Garmin Auth has not been initialized, cannot provide MFA token yet.");

		if (partialAuth.AuthStage != AuthStage.NeedMfaToken)
			throw new ArgumentException($"We're in the wrong GarminAuthStage, expected NeedMfaToken but found {partialAuth.AuthStage}");

		var mfaData = new List<KeyValuePair<string, string>>()
		{
			new KeyValuePair<string, string>("embed", "true"),
			new KeyValuePair<string, string>("mfa-code", mfaCode),
			new KeyValuePair<string, string>("fromPage", "setupEnterMfaCode"),
			new KeyValuePair<string, string>("_csrf", partialAuth.MFACsrfToken)
		};

		/////////////////////////////////
		// Send the MFA Code to Garmin
		////////////////////////////////
		try
		{
			SendMFAResult mfaResponse = new();
			var jar = CookieJar.LoadFromString(partialAuth.CookieJarString);
			mfaResponse.RawResponseBody = await _apiClient.SendMfaCodeAsync(CommonQueryParams, mfaData, jar);
			return await CompleteGarminAuthenticationAsync(mfaResponse.RawResponseBody);
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

	private async Task<OAuth1Token> GetOAuth1Async(string ticket, ConsumerCredentials credentials)
	{
		string oauth1Response = null;
		try
		{
			oauth1Response = await _apiClient.GetOAuth1TokenAsync(credentials, ticket);
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

		return new OAuth1Token()
		{
			Token = oAuthToken,
			TokenSecret = oAuthTokenSecret
		};
	}

	public async Task<bool> SignOutAsync()
	{
		await _garminDb.UpsertPartialGarminAuthenticationAsync(1, null);
		await _garminDb.UpsertGarminOAuth1TokenAsync(1, null);
		await _garminDb.UpsertGarminOAuth2TokenAsync(1, null);

		return true;
	}
}
