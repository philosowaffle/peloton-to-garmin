using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using Peloton.Dto;
using Serilog;

namespace Peloton.Auth;

public interface IPelotonAuthApiClient
{
    Task<PelotonApiAuthentication> Authenticate();
}

public class PelotonAuthApiClient : IPelotonAuthApiClient
{
    private static readonly ILogger _logger = LogContext.ForClass<PelotonAuthApiClient>();

    private readonly ISettingsService _settingsService;

    public PelotonAuthApiClient(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<PelotonApiAuthentication> Authenticate()
    {
        var settings = (await _settingsService.GetSettingsAsync()).Peloton;
        settings.EnsurePelotonCredentialsAreProvided();

        try
        {
            // If the user hard coded their Bearer token
            // then use that
            if (!string.IsNullOrWhiteSpace(settings.BearerToken))           
                return await ConstructAndSaveAuth(settings, settings.BearerToken);

            // If we have an existing valid auth token
            // then reuse it
            var existingAuthentication = _settingsService.GetPelotonApiAuthentication(settings.Email);
            if (existingAuthentication is object && existingAuthentication.IsValid(settings))
                return existingAuthentication;

            // If we have a refresh token then
            // use that
            if (existingAuthentication is object && !string.IsNullOrWhiteSpace(existingAuthentication.Token?.RefreshToken))
                return await ConstructAndSaveAuth(settings, await RefreshOAuthToken(settings.Api, existingAuthentication.Token));

            // Finally, do full login flow
            return await ConstructAndSaveAuth(settings, await LoginWithOAuthAsync(settings));
        }
        catch (FlurlHttpException fe) when (fe.StatusCode == (int)HttpStatusCode.Unauthorized)
        {
            _logger.Error(fe, $"Failed to authenticate with Peloton.");
            _settingsService.ClearPelotonApiAuthentication(settings.Email);
            throw new PelotonAuthenticationError("Failed to authenticate with Peloton. Please confirm your Peloton Email and Password are correct.", fe);
        }
        catch (Exception e)
        {
            _logger.Fatal(e, $"Failed to authenticate with Peloton.");
            _settingsService.ClearPelotonApiAuthentication(settings.Email);
            throw;
        } 
    }

    private async Task<Token> LoginWithOAuthAsync(PelotonSettings settings)
    {
        var config = GenerateOAuthConfig(settings.Api);
        return await PerformOAuthLoginAsync(config, settings);
    }

    private async Task<Token> RefreshOAuthToken(PelotonApiSettings settings, Token currentToken)
    {
        return await (await $"https://{settings.AuthDomain}{settings.AuthTokenPath}"
            .WithHeader("Content-Type", "application/x-www-form-urlencoded; charset=utf-8")
            .PostUrlEncodedAsync(new
            {
                grant_type = "refresh_token",
                client_id = settings.AuthClientId,
                refresh_token = currentToken.RefreshToken
            })).GetJsonAsync<Token>();
    }

    private async Task<Token> PerformOAuthLoginAsync(OAuthConfig config, PelotonSettings pelotonSettings)
    {
        var authorizeUrl = BuildAuthorizeUrl(config, pelotonSettings.Api);
        var session = await InitiateAuthFlowAsync(authorizeUrl, config);
        
        if (!string.IsNullOrEmpty(session.State))
            config.State = session.State;

        var nextUrl = await SubmitCredentialsAsync(session, config, pelotonSettings);
        var code = await FollowAuthorizationRedirectsAsync(nextUrl, config);
        var token = await ExchangeCodeForTokenAsync(code, config);

        return token;
    }

    private async Task<AuthSession> InitiateAuthFlowAsync(string authorizeUrl, OAuthConfig config)
    {
        string loginUrl = null;

        var response = await authorizeUrl
            .WithAutoRedirect(true)
            .WithCookies(config.CookieJar)
            .WithAutoRedirect(true)
            .OnRedirect(call =>
            {
                if (string.IsNullOrEmpty(loginUrl) && call.Redirect?.Url != null)
                {
                    loginUrl = call.Redirect.Url;
                }
            })
            .AllowAnyHttpStatus()
            .GetAsync();

        if (string.IsNullOrEmpty(loginUrl))
        {
            loginUrl = response.ResponseMessage.RequestMessage?.RequestUri?.ToString();
        }

        if (string.IsNullOrEmpty(loginUrl))
            throw new InvalidOperationException("Authorize redirect missing location");

        // Extract state from URL if present
        var parsedUrl = new Url(loginUrl);
        var qsState = parsedUrl.QueryParams.FirstOrDefault(q => q.Name == "state").Value?.ToString();
        if (!string.IsNullOrEmpty(qsState))
            config.State = qsState;

        // Extract CSRF token from cookies
        var authUrl = $"https://{config.AuthDomain}";
        var csrfCookie = config.CookieJar.FirstOrDefault(c => c.OriginUrl.Root == authUrl
                                                        && c.Path == "/usernamepassword/login"
                                                        && c.Name == "_csrf");

        if (csrfCookie == null || string.IsNullOrEmpty(csrfCookie.Value))
            throw new InvalidOperationException("Missing CSRF token");

        return new AuthSession
        {
            LoginUrl = loginUrl,
            State = config.State,
            CsrfToken = csrfCookie.Value
        };
    }

    private async Task<string> SubmitCredentialsAsync(AuthSession session, OAuthConfig config, PelotonSettings pelotonSettings)
    {
        var payload = new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = config.RedirectUri,
            ["tenant"] = "peloton-prod",
            ["response_type"] = "code",
            ["scope"] = config.Scope,
            ["audience"] = config.Audience,
            ["_csrf"] = session.CsrfToken,
            ["state"] = session.State,
            ["_intstate"] = "deprecated",
            ["nonce"] = config.Nonce,
            ["username"] = pelotonSettings.Email,
            ["password"] = pelotonSettings.Password,
            ["connection"] = "pelo-user-password",
            ["code_challenge"] = config.CodeChallenge,
            ["code_challenge_method"] = config.CodeChallengeMethod
        };

        var loginEndpoint = $"https://{config.AuthDomain}/usernamepassword/login";
        
        var response = await loginEndpoint
            .WithCookies(config.CookieJar)
            .WithHeader("Content-Type", "application/json")
            .WithHeader("Accept", "*/*")
            .WithHeader("Origin", $"https://{config.AuthDomain}")
            .WithHeader("Referer", session.LoginUrl)
            .WithHeader("Auth0-Client", pelotonSettings.Api.Auth0ClientPayload)
            .WithAutoRedirect(false)
            .AllowAnyHttpStatus()
            .PostJsonAsync(payload);

        var location = response.Headers.FirstOrDefault("Location");
        if (!string.IsNullOrEmpty(location))
        {
            return EnsureAbsoluteUrl(config.AuthDomain, location);
        }

        // Parse hidden form from response
        var bodyText = await response.GetStringAsync();
        var (actionNext, hiddenFields) = ParseHiddenForm(bodyText);

        return await SubmitHiddenFormAsync(actionNext, hiddenFields, config);
    }

    private async Task<string> SubmitHiddenFormAsync(string action, Dictionary<string, string> fields, OAuthConfig config)
    {
        var actionUrl = action;
        if (!actionUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            actionUrl = EnsureAbsoluteUrl(config.AuthDomain, action);
        }

        var response = await actionUrl
            .WithCookies(config.CookieJar)
            .WithHeader("Content-Type", "application/x-www-form-urlencoded")
            .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:145.0) Gecko/20100101 Firefox/145.0")
            .WithAutoRedirect(true)
            .AllowAnyHttpStatus()
            .PostUrlEncodedAsync(fields);

        var location = response.Headers.FirstOrDefault("Location");
        if (!string.IsNullOrEmpty(location))
        {
            return EnsureAbsoluteUrl(config.AuthDomain, location);
        }

        return response.ResponseMessage.RequestMessage?.RequestUri?.ToString() ?? actionUrl;
    }

    private async Task<string> FollowAuthorizationRedirectsAsync(string startUrl, OAuthConfig config)
    {
        string callbackUrl = null;
        string code = null;
        string state = null;

        var response = await startUrl
            .WithCookies(config.CookieJar)
            .WithAutoRedirect(true)
            .OnRedirect(call =>
            {
                var redirectUrl = new Url(call.Redirect.Url);
                var codeParam = redirectUrl.QueryParams.FirstOrDefault(q => q.Name == "code").Value?.ToString();

                if (!string.IsNullOrEmpty(codeParam))
                {
                    callbackUrl = call.Redirect.Url;
                    code = codeParam;
                    state = redirectUrl.QueryParams.FirstOrDefault(q => q.Name == "state").Value?.ToString();
                }
            })
            .AllowAnyHttpStatus()
            .GetAsync();

        // Check final URL
        if (string.IsNullOrEmpty(code) && response.ResponseMessage.RequestMessage?.RequestUri != null)
        {
            var finalUrl = new Url(response.ResponseMessage.RequestMessage.RequestUri.ToString());
            code = finalUrl.QueryParams.FirstOrDefault(q => q.Name == "code").Value?.ToString();
            state = finalUrl.QueryParams.FirstOrDefault(q => q.Name == "state").Value?.ToString();
        }

        // Check Location header
        if (string.IsNullOrEmpty(code))
        {
            var location = response.Headers.FirstOrDefault("Location");
            if (!string.IsNullOrEmpty(location))
            {
                var locationUrl = new Url(location);
                code = locationUrl.QueryParams.FirstOrDefault(q => q.Name == "code").Value?.ToString();
                state = locationUrl.QueryParams.FirstOrDefault(q => q.Name == "state").Value?.ToString();
            }
        }

        if (string.IsNullOrEmpty(code))
        {
            var bodyText = await response.GetStringAsync();
            throw new InvalidOperationException(
                $"Authorization code not found (status {response.StatusCode}): {bodyText}");
        }

        if (!string.IsNullOrEmpty(state))
            config.State = state;

        return code;
    }

    private async Task<Token> ExchangeCodeForTokenAsync(string code, OAuthConfig config)
    {
        var endpoint = $"https://{config.AuthDomain}{config.AuthTokenPath}";
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = config.ClientId,
            ["code_verifier"] = config.CodeVerifier,
            ["code"] = code,
            ["redirect_uri"] = config.RedirectUri
        };

        var response = await endpoint
            .WithCookies(config.CookieJar)
            .WithHeader("Content-Type", "application/json")
            .WithHeader("Accept", "application/json")
            .AllowAnyHttpStatus()
            .PostJsonAsync(payload);

        if (response.StatusCode >= 400)
        {
            var errorBody = await response.GetStringAsync();
            throw new InvalidOperationException(
                $"Token exchange failed: {Truncate(errorBody, 500)}");
        }

        var tokenResponse = await response.GetJsonAsync<Token>();
        
        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new InvalidOperationException("Token exchange response missing access token");

        return tokenResponse;
    }

    private static OAuthConfig GenerateOAuthConfig(PelotonApiSettings pelotonApiSettings)
    {
        var verifier = GenerateRandomString(64);
        var challenge = GenerateCodeChallenge(verifier);
        var state = GenerateRandomString(32);
        var nonce = GenerateRandomString(32);

        return new OAuthConfig
        {
            ClientId = pelotonApiSettings.AuthClientId,
            CodeVerifier = verifier,
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            State = state,
            Nonce = nonce,
            RedirectUri = pelotonApiSettings.AuthRedirectUri,
            Audience = pelotonApiSettings.AuthAudience,
            Scope = pelotonApiSettings.AuthScope,
            AuthDomain = pelotonApiSettings.AuthDomain,
            AuthTokenPath = pelotonApiSettings.AuthTokenPath
        };
    }

    private static string GenerateRandomString(int length)
    {
        var bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        var encoded = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        return encoded.Substring(0, Math.Min(length, encoded.Length));
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }

    private static string BuildAuthorizeUrl(OAuthConfig config, PelotonApiSettings apiSettings)
    {
        return $"https://{config.AuthDomain}{apiSettings.AuthAuthorizePath}"
            .SetQueryParam("client_id", config.ClientId)
            .SetQueryParam("audience", config.Audience)
            .SetQueryParam("scope", config.Scope)
            .SetQueryParam("response_type", "code")
            .SetQueryParam("response_mode", "query")
            .SetQueryParam("redirect_uri", config.RedirectUri)
            .SetQueryParam("state", config.State)
            .SetQueryParam("nonce", config.Nonce)
            .SetQueryParam("code_challenge", config.CodeChallenge)
            .SetQueryParam("code_challenge_method", config.CodeChallengeMethod)
            .SetQueryParam("auth0Client", apiSettings.Auth0ClientPayload);
    }

    private static (string action, Dictionary<string, string> fields) ParseHiddenForm(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var form = doc.DocumentNode.SelectSingleNode("//form");
        if (form == null)
            throw new InvalidOperationException("Hidden form had no action");

        var action = form.GetAttributeValue("action", "");
        if (string.IsNullOrEmpty(action))
            throw new InvalidOperationException("Hidden form had no action");

        var fields = new Dictionary<string, string>();
        var hiddenInputs = form.SelectNodes(".//input[@type='hidden']");
        
        if (hiddenInputs != null)
        {
            foreach (var input in hiddenInputs)
            {
                var name = input.GetAttributeValue("name", "");
                var value = input.GetAttributeValue("value", "");
                if (!string.IsNullOrEmpty(name))
                {
                    // Decode HTML entities (e.g., &#34; -> ")
                    fields[name] = System.Net.WebUtility.HtmlDecode(value);
                }
            }
        }

        return (action, fields);
    }

    private static string EnsureAbsoluteUrl(string domain, string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        return $"https://{domain}{(path.StartsWith("/") ? path : "/" + path)}";
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength);
    }

    private async Task<PelotonApiAuthentication> ConstructAndSaveAuth(PelotonSettings settings, Token bearerToken)
    {
        var auth = new PelotonApiAuthentication()
        {
            Email = settings.Email,
            Password = settings.Password,
            Token = bearerToken,
            UserId = await GetUserId(bearerToken.AccessToken, settings.Api),
            ExpiresAt = DateTime.UtcNow.AddHours(bearerToken.ExpiresIn),
        };
        _settingsService.SetPelotonApiAuthentication(auth);
        return auth;
    }

    private async Task<PelotonApiAuthentication> ConstructAndSaveAuth(PelotonSettings settings, string bearerToken)
    {
        var auth = new PelotonApiAuthentication()
        {
            Email = settings.Email,
            Password = settings.Password,
            Token = new() { AccessToken = bearerToken },
            UserId = await GetUserId(bearerToken, settings.Api),
            ExpiresAt = DateTime.UtcNow.AddHours(settings.Api.BearerTokenDefaultTtlSeconds).AddHours(-1), // refresh early
        };
        _settingsService.SetPelotonApiAuthentication(auth);
        return auth;
    }

    private async Task<string> GetUserId(string bearerToken, PelotonApiSettings settings)
    {
        return (await $"{settings.ApiUrl}api/me"
                            .WithOAuthBearerToken(bearerToken)
                            .WithCommonHeaders()
                            .GetJsonAsync<UserData>()).Id;
    } 

    private class OAuthConfig
    {
        public string ClientId { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
        public string CodeVerifier { get; set; }
        public string State { get; set; }
        public string Nonce { get; set; }
        public string RedirectUri { get; set; }
        public string Audience { get; set; }
        public string Scope { get; set; }
        public string AuthDomain { get; set; }
        public string AuthTokenPath { get; set; }
        public CookieJar CookieJar { get; set; } = new CookieJar();
    }

    private class AuthSession
    {
        public string LoginUrl { get; set; }
        public string State { get; set; }
        public string CsrfToken { get; set; }
    }
}      