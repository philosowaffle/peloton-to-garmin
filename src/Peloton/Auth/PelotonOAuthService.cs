using Common.Dto;
using Common.Observe;
using Common.Service;
using Flurl.Http;
using Peloton.Dto;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Peloton.Auth
{
	public interface IPelotonOAuthService
	{
		Task<PelotonOAuthTokenResponse> AuthenticateAsync(string email, string password);
	}

	public class PelotonOAuthService : IPelotonOAuthService
	{
		private static readonly ILogger _logger = LogContext.ForClass<PelotonOAuthService>();
		private const int MaxRedirects = 10;
		private const string AuthorizationUrl = "https://auth.onepeloton.com/authorize";
		private const string TokenUrl = "https://api.onepeloton.com/auth/token";
		private const string ClientId = "8a7f8c12-6a5e-4c9e-8f3b-1d2e3f4a5b6c"; // This may need to be updated

		private readonly ISettingsService _settingsService;

		public PelotonOAuthService(ISettingsService settingsService)
		{
			_settingsService = settingsService;
		}

		public async Task<PelotonOAuthTokenResponse> AuthenticateAsync(string email, string password)
		{
			try
			{
				_logger.Information("Starting Peloton OAuth PKCE authentication flow");

				// Step 1: Generate PKCE parameters
				var oauthConfig = PelotonOAuthConfig.Generate();
				_logger.Debug("Generated PKCE parameters");

				// Step 2: Create cookie jar for session management
				var cookieJar = new CookieJar();

				// Step 3: Initiate OAuth flow and follow redirects
				var authCode = await InitiateOAuthFlowAsync(email, password, oauthConfig, cookieJar);
				_logger.Debug("Successfully obtained authorization code");

				// Step 4: Exchange authorization code for access token
				var token = await ExchangeCodeForTokenAsync(authCode, oauthConfig);
				_logger.Information("Successfully obtained OAuth access token");

				return token;
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to authenticate with Peloton OAuth");
				throw;
			}
		}

		private async Task<string> InitiateOAuthFlowAsync(string email, string password, PelotonOAuthConfig config, CookieJar cookieJar)
		{
			// Build authorization URL with PKCE parameters
			var authUrl = $"{config.AuthorizationEndpoint}?" + string.Join("&", new[]
			{
				$"client_id={Uri.EscapeDataString(config.ClientId)}",
				$"response_type=code",
				$"redirect_uri={Uri.EscapeDataString(config.RedirectUri)}",
				$"scope={Uri.EscapeDataString(config.Scope)}",
				$"state={Uri.EscapeDataString(config.State)}",
				$"nonce={Uri.EscapeDataString(config.Nonce)}",
				$"code_challenge={Uri.EscapeDataString(config.CodeChallenge)}",
				$"code_challenge_method=S256",
				$"audience={Uri.EscapeDataString(config.Audience)}"
			});

			_logger.Debug($"Initiating OAuth flow to: {authUrl}");

			// Make initial request to authorization endpoint
			var response = await authUrl
				.WithCookies(cookieJar)
				.WithHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36")
				.AllowAnyHttpStatus()
				.GetAsync();

			// Follow redirects and extract login form
			string currentUrl = response.ResponseMessage.RequestMessage.RequestUri.ToString();
			var htmlContent = await response.GetStringAsync();

			// Extract CSRF token and form action URL from the login page
			var csrfToken = ExtractCsrfToken(htmlContent);
			var formAction = ExtractFormAction(htmlContent);

			if (string.IsNullOrEmpty(csrfToken))
			{
				_logger.Warning("Could not extract CSRF token from login page");
			}

			// Submit credentials
			var loginResponse = await SubmitCredentialsAsync(config.LoginEndpoint, email, password, config, cookieJar);

			// Follow redirects to capture authorization code
			var authCode = await FollowRedirectsForAuthCodeAsync(loginResponse, config, cookieJar);

			return authCode;
		}

			private async Task<IFlurlResponse> SubmitCredentialsAsync(string loginUrl, string email, string password, PelotonOAuthConfig config, CookieJar cookieJar)
	{
		_logger.Debug("Submitting credentials to Peloton Auth0");

		// Auth0 requires JSON payload, not form-encoded
		var loginPayload = new
		{
			client_id = config.ClientId,
			username = email,
			password = password,
			connection = "pelo-user-password",  // Critical: Auth0 connection name
			code_challenge = config.CodeChallenge,  // Send challenge in login request
			state = config.State,  // Include state
			tenant = "onepeloton-production",
			_intstate = "deprecated",
			scope = config.Scope,
			response_type = "code",
			redirect_uri = config.RedirectUri
		};

		// Auth0-Client header (base64 encoded JSON)
		var auth0ClientInfo = Convert.ToBase64String(
			System.Text.Encoding.UTF8.GetBytes("{\"name\":\"auth0-spa-js\",\"version\":\"1.22.0\"}")
		);

		var response = await loginUrl
			.WithCookies(cookieJar)
			.WithHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36")
			.WithHeader("Auth0-Client", auth0ClientInfo)
			.WithHeader("Content-Type", "application/json")
			.WithHeader("Origin", "https://auth.onepeloton.com")
			.WithHeader("Referer", "https://auth.onepeloton.com/")
			.AllowAnyHttpStatus()
			.PostJsonAsync(loginPayload);

		var statusCode = response.StatusCode;
		_logger.Debug($"Login response status: {statusCode}");

		if (statusCode == (int)HttpStatusCode.Unauthorized || statusCode == (int)HttpStatusCode.Forbidden)
		{
			throw new PelotonAuthenticationError("Invalid email or password");
		}

		// Auth0 might return HTML with hidden form fields to resubmit
		if (response.ResponseMessage.Content.Headers.ContentType?.MediaType == "text/html")
		{
			var html = await response.GetStringAsync();
			var formAction = ExtractFormAction(html);
			var hiddenFields = ExtractHiddenFormFields(html);

			if (!string.IsNullOrEmpty(formAction) && hiddenFields.Count > 0)
			{
				_logger.Debug($"Resubmitting hidden form to: {formAction}");
				response = await formAction
					.WithCookies(cookieJar)
					.WithHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36")
					.WithHeader("Content-Type", "application/x-www-form-urlencoded")
					.AllowAnyHttpStatus()
					.PostUrlEncodedAsync(hiddenFields);
			}
		}

		return response;
	}

		private async Task<string> FollowRedirectsForAuthCodeAsync(IFlurlResponse initialResponse, PelotonOAuthConfig config, CookieJar cookieJar)
		{
			_logger.Debug("Following redirects to capture authorization code");

			var currentResponse = initialResponse;
			var redirectCount = 0;

			while (redirectCount < MaxRedirects)
			{
				var location = currentResponse.Headers.FirstOrDefault("Location");
				var currentUrl = currentResponse.ResponseMessage.RequestMessage.RequestUri.ToString();

				// Check if we're at the callback URL with the authorization code
				if (currentUrl.Contains(config.RedirectUri) || (!string.IsNullOrEmpty(location) && location.Contains(config.RedirectUri)))
				{
					var urlToCheck = !string.IsNullOrEmpty(location) ? location : currentUrl;
					var uri = new Uri(urlToCheck);
					var query = HttpUtility.ParseQueryString(uri.Query);
					var code = query["code"];

					if (!string.IsNullOrEmpty(code))
					{
						_logger.Debug("Successfully captured authorization code");
						return code;
					}
				}

				// Check for error in response
				if (currentUrl.Contains("error=") || (!string.IsNullOrEmpty(location) && location.Contains("error=")))
				{
					var urlToCheck = !string.IsNullOrEmpty(location) ? location : currentUrl;
					var uri = new Uri(urlToCheck);
					var query = HttpUtility.ParseQueryString(uri.Query);
					var error = query["error"];
					var errorDescription = query["error_description"];
					throw new PelotonAuthenticationError($"OAuth error: {error} - {errorDescription}");
				}

				// No more redirects
				if (string.IsNullOrEmpty(location))
				{
					break;
				}

				// Follow the redirect
				_logger.Debug($"Following redirect to: {location}");
				currentResponse = await location
					.WithCookies(cookieJar)
					.WithHeader("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36")
					.AllowAnyHttpStatus()
					.GetAsync();

				redirectCount++;
			}

			throw new PelotonAuthenticationError("Failed to obtain authorization code - redirect limit exceeded or code not found");
		}

		private async Task<PelotonOAuthTokenResponse> ExchangeCodeForTokenAsync(string authCode, PelotonOAuthConfig config)
		{
			_logger.Debug("Exchanging authorization code for access token");

			var tokenRequest = new
			{
				grant_type = "authorization_code",
				code = authCode,
				redirect_uri = config.RedirectUri,
				client_id = config.ClientId,
				code_verifier = config.CodeVerifier
			};

			var response = await config.TokenEndpoint
				.WithHeader("Content-Type", "application/json")
				.PostJsonAsync(tokenRequest)
				.ReceiveJson<PelotonOAuthTokenResponse>();

			response.SetExpiresAt();
			return response;
		}

		private string ExtractCsrfToken(string html)
		{
			// Try multiple patterns for CSRF token extraction
			var patterns = new[]
			{
				@"name=[""']_csrf[""']\s+value=[""']([^""']+)[""']",
				@"name=[""']csrf_token[""']\s+value=[""']([^""']+)[""']",
				@"<input[^>]*name=[""']_csrf[""'][^>]*value=[""']([^""']+)[""']",
				@"""csrf""\s*:\s*""([^""]+)""",
			};

			foreach (var pattern in patterns)
			{
				var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					return match.Groups[1].Value;
				}
			}

			return null;
		}

		private string ExtractFormAction(string html)
		{
			var match = Regex.Match(html, @"<form[^>]*action=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
			return match.Success ? match.Groups[1].Value : null;
	private Dictionary<string, string> ExtractHiddenFormFields(string html)
	{
		var fields = new Dictionary<string, string>();
		var matches = Regex.Matches(html, @"<input[^>]*type=[""]hidden[""][^>]*name=[""]([^""]+)[""][^>]*value=[""]([^""]*)[""]", RegexOptions.IgnoreCase);

		foreach (Match match in matches)
		{
			if (match.Groups.Count >= 3)
			{
				fields[match.Groups[1].Value] = match.Groups[2].Value;
			}
		}

		// Also try reverse pattern (value before name)
		var reverseMatches = Regex.Matches(html, @"<input[^>]*value=[""]([^""]*)[""][^>]*name=[""]([^""]+)[""][^>]*type=[""]hidden[""]", RegexOptions.IgnoreCase);

		foreach (Match match in reverseMatches)
		{
			if (match.Groups.Count >= 3)
			{
				fields[match.Groups[2].Value] = match.Groups[1].Value;
			}
		}

		_logger.Debug($"Extracted {fields.Count} hidden form fields");
		return fields;
	}


}