using Common.Service;
using Common.Stateful;
using Flurl.Http;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Garmin.Auth;

public interface IGarminOAuthService
{

}

public class GarminOAuthService : IGarminOAuthService
{
	private readonly ISettingsService _settingsService;
	private readonly IGarminApiClient _apiClient;

	public GarminOAuthService(ISettingsService settingsService, IGarminApiClient apiClient)
	{
		_settingsService = settingsService;
		_apiClient = apiClient;
	}

	private async Task GetAuthTokenAsync()
	{
		var auth = new GarminApiAuthentication();
		auth.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";


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
			embed = "true",
			gauthHost = "https://sso.garmin.com/sso",
			service = "https://sso.garmin.com/sso/embed",
			source = "https://sso.garmin.com/sso/embed",
			redirectAfterAccountLoginUrl = "https://sso.garmin.com/sso/embed",
			redirectAfterAccountCreationUrl = "https://sso.garmin.com/sso/embed",
		};

		var tokenResult = await _apiClient.GetCsrfTokenAsync(auth, csrfRequest, jar);
		var tokenRegex = new Regex("name=\"_csrf\"\\s+value=\"(.+?)\"");
		var match = tokenRegex.Match(tokenResult.RawResponseBody);
		if (!match.Success)
			throw new Exception("Failed to regex match token");

		var csrfToken = match.Groups.Values.First();

		/////////////////////////////////
		// Submit login form
		////////////////////////////////
		var loginData = new
		{
			username = "email",
			passowrd = "password",
			embed = "true",
			_csrf = csrfToken
		};
		var signInResult = await _apiClient.SendCredentialsAsync(auth, csrfRequest, loginData, jar);

		if (signInResult.WasRedirected && signInResult.RedirectedTo.Contains("https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"))
		{
			// todo: handle mfa flow
			throw new NotImplementedException("handle mfa");
		}

		var ticketRegex = new Regex("embed\\?ticket=([^\"]+)\"");
		var ticketMatch = ticketRegex.Match(signInResult.RawResponseBody);
		if (!ticketMatch.Success)
			throw new Exception("Filed to find post signin ticket.");

		var ticket = ticketMatch.Groups.Values.First();

		/////////////////////////////////
		// Get OAuth Tokens
		////////////////////////////////

		// TODO: fetch id and secret from garth hosted file
		var c = new FlurlClient();
		var result = await c
			.WithHeader("User-Agent", auth.UserAgent)
			.HttpClient
			.RequestTokenAsync(new TokenRequest()
			{
				Address = $"https://connectapi.garmin.com/oauth-service/oauth/preauthorized?ticket={ticket}&login-url=https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true",
				ClientId = "fc3e99d2-118c-44b8-8ae3-03370dde24c0",
				ClientSecret = "E08WAR897WEy2knn7aFBrvegVAf0AFdWBBF"
			});

	}
}
