using Common.Service;
using Common.Stateful;
using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

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
		// Get CSRF token
		////////////////////////////////
	}
}
