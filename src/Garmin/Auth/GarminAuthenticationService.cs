using Common.Observe;
using Common.Service;
using Common.Stateful;
using Flurl.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Garmin.Auth;

public interface IGarminAuthenticationService
{
	Task<GarminApiAuthentication> GetGarminAuthentication();
}

public class GarminAuthenticationService : IGarminAuthenticationService
{
	private static readonly ILogger _logger = LogContext.ForClass<GarminAuthenticationService>();

	private readonly ISettingsService _settingsService;
	private readonly IGarminApiClient _apiClient;

	public GarminAuthenticationService(ISettingsService settingsService, IGarminApiClient apiClient)
	{
		_settingsService = settingsService;
		_apiClient = apiClient;
	}

	public async Task<GarminApiAuthentication> GetGarminAuthentication()
	{
		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin.EnsureGarminCredentialsAreProvided();

		var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);
		if (auth is object && auth.IsValid(settings))
			return auth;

		auth = new();
		auth.Email = settings.Garmin.Email;
		auth.Password = settings.Garmin.Password;
		CookieJar jar = null;

		var appConfig = await _settingsService.GetAppConfigurationAsync();
		if (!string.IsNullOrEmpty(appConfig.Developer.UserAgent))
			auth.UserAgent = appConfig.Developer.UserAgent;

		object queryParams = new
		{
			clientId = "GarminConnect",
			consumeServiceTicket = "false",
			createAccountShown = "true",
			cssUrl = "https://static.garmincdn.com/com.garmin.connect/ui/css/gauth-custom-v1.2-min.css",
			displayNameShown = "false",
			embedWidget = "false",
			gauthHost = "https://sso.garmin.com/sso",
			generateExtraServiceTicket = "true",
			generateTwoExtraServiceTickets = "true",
			generateNoServiceTicket = "false",
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

		/////////////////////////////////
		// Init Auth Flow
		////////////////////////////////
		try
		{
			await _apiClient.InitSigninFlowAsync(queryParams, auth.UserAgent, out jar);
		}
		catch (FlurlHttpException e)
		{
			throw new GarminAuthenticationError("Failed to initialize sign in flow.", e) { Code = Code.FailedPriorToCredentialsUsed };
		}

		object loginData = new
		{
			embed = "true",
			username = auth.Email,
			password = auth.Password,
			lt = "e1s1",
			_eventId = "submit",
			displayNameRequired = "false",
		};

		/////////////////////////////////
		// Send Credentials
		////////////////////////////////
		SendCredentialsResult sendCredentialsResult = null;
		try
		{
			sendCredentialsResult = await _apiClient.SendCredentialsAsync(auth, queryParams, loginData, jar);
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
		SendMFAResult sendMfaResult = null;
		if (sendCredentialsResult.WasRedirected && sendCredentialsResult.RedirectedTo.Contains("https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"))
		{
			sendMfaResult = await HandleMFAAsync(sendCredentialsResult.RawResponseBody, auth.UserAgent, queryParams, jar);
		}

		//////////////////////////////////////////////////////////
		// Ensure CookieJar looks good and we have Service Ticket
		//////////////////////////////////////////////////////////
		// Check we have SSO guid in the cookies
		if (!jar.Any(c => c.Name == "GARMIN-SSO-GUID"))
			throw new GarminAuthenticationError("Auth appeared successful but failed to find Garmin auth cookie.") { Code = Code.AuthAppearedSuccessful };

		// Try to find the full post login ServiceTicket
		var loginResult = sendMfaResult?.RawResponseBody ?? sendCredentialsResult?.RawResponseBody;
		var regex2 = new Regex("var response_url(\\s+) = (\\\"|\\').*?ticket=(?<ticket>[\\w\\-]+)(\\\"|\\')");
		var match = regex2.Match(loginResult);
		if (!match.Success)
			throw new GarminAuthenticationError("Auth appeared successful but failed to find the service ticket.") { Code = Code.AuthAppearedSuccessful };

		var ticket = match.Groups.GetValueOrDefault("ticket")?.Value;
		_logger.Verbose($"Service Ticket: {ticket}");
		if (string.IsNullOrWhiteSpace(ticket))
			throw new GarminAuthenticationError("Auth appeared successful, and found service ticket, but ticket was null or empty.") { Code = Code.AuthAppearedSuccessful };

		////////////////////////////////////////////
		// Send the ServiceTicket - Completes Auth
		///////////////////////////////////////////
		try
		{
			var serviceTicketResponse = await _apiClient.SendServiceTicketAsync(auth.UserAgent, ticket, jar);

			if (serviceTicketResponse.StatusCode == (int)HttpStatusCode.Moved)
				throw new GarminAuthenticationError("Auth appeared successful but Garmin did not accept service ticket.") { Code = Code.AuthAppearedSuccessful };
		}
		catch (FlurlHttpException e)
		{
			throw new GarminAuthenticationError("Auth appeared successful but there was an error sending the service ticket.", e) { Code = Code.AuthAppearedSuccessful };
		}

		auth.CookieJar = jar;
		_settingsService.SetGarminAuthentication(auth);
		return auth;
	}

	private async Task<SendMFAResult> HandleMFAAsync(string sendCredentialsResponseBody, string userAgent, object queryParams, CookieJar jar)
	{
		/////////////////////////////////
		// Try to find the csfr Token
		////////////////////////////////
		var regex3 = new Regex("name=\"_csrf\"\\s+value=\"(?<csrf>[A-Z0-9]+)");
		var match3 = regex3.Match(sendCredentialsResponseBody);
		if (!match3.Success)
			throw new GarminAuthenticationError("MFA: Failed to find csrf token.") { Code = Code.FailedPriorToMfaUsed };

		var csrfToken = match3.Groups.GetValueOrDefault("csrf")?.Value;
		_logger.Verbose($"_csrf Token: {csrfToken}");
		if (string.IsNullOrEmpty(csrfToken))
			throw new GarminAuthenticationError("MFA: Found csrf token but it was null or empty.") { Code = Code.FailedPriorToMfaUsed };

		/////////////////////////////////
		// Ask for MFA Code
		////////////////////////////////
		Console.WriteLine("MFA Enabled. Please check your email or phone for the authenctaion code sent by Garmin.");
		var mfaCode = string.Empty;
		var retryCount = 5;
		while (retryCount > 0 && string.IsNullOrWhiteSpace(mfaCode))
		{
			Console.Write("Enter MFA Code: ");
			mfaCode = Console.ReadLine();
			retryCount--;
		}

		var mfaData = new List<KeyValuePair<string, string>>()
		{
			new KeyValuePair<string, string>("embed", "false"),
			new KeyValuePair<string, string>("mfa-code", mfaCode),
			new KeyValuePair<string, string>("fromPage", "setupEnterMfaCode"),
			new KeyValuePair<string, string>("_csrf", csrfToken)
		};

		/////////////////////////////////
		// Send the MFA Code to Garmin
		////////////////////////////////
		try
		{
			SendMFAResult mfaResponse = new();
			mfaResponse.RawResponseBody = await _apiClient.SendMfaCodeAsync(userAgent, queryParams, mfaData, jar);
			return mfaResponse;
		}
		catch (FlurlHttpException e) when (e.StatusCode is (int)HttpStatusCode.Forbidden)
		{
			var responseContent = (await e.GetResponseStringAsync()) ?? string.Empty;

			if (responseContent == "error code: 1020")
				throw new GarminAuthenticationError("MFA: Garmin Authentication Failed. Blocked by CloudFlare.", e) { Code = Code.Cloudflare };

			throw new GarminAuthenticationError("MFA: MFA Code rejected by Garmin.", e) { Code = Code.InvalidMfaCode };
		}
	}
}
