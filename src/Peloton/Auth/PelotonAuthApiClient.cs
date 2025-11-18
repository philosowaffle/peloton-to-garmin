using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;

namespace Peloton.Auth;
public class PelotonAuthApiClient
{
    private const string DefaultBaseUrl = "https://api.onepeloton.com/";
    private const string AuthDomain = "auth.onepeloton.com";

    // from browser: "q6lqsS8VoP0OzCNJ5PmbglDGkOD7NxxV";
    private const string AuthClientId =  "WVoJxVDdPoFx4RNewvvg6ch2mZ7bwnsM";
    private const string AuthAudience = "https://api.onepeloton.com/";
    private const string AuthScope = "offline_access openid peloton-api.members:default";
    private const string AuthRedirectUri = "https://members.onepeloton.com/callback";
    private const string AuthMembersOrigin = "https://members.onepeloton.com";
    private const string Auth0ClientPayload = "eyJuYW1lIjoiYXV0aDAtc3BhLWpzIiwidmVyc2lvbiI6IjIuMS4zIn0=";
    private const string AuthAuthorizePath = "/authorize";
    private const string AuthTokenPath = "/oauth/token";
    private const int AuthRedirectLogLimit = 5000;

    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;
    private CookieJar _cookieContainer;
    private string _bearer;

    public PelotonAuthApiClient(string username, string password)
    {
        _baseUrl = DefaultBaseUrl;
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _cookieContainer = new CookieJar();
    }

    public async Task LoginWithOAuthAsync()
    {
        var config = GenerateOAuthConfig();
        var token = await PerformOAuthLoginAsync(config);
        _bearer = token;
    }

    public async Task<Dictionary<string, object>> GetMyInfoAsync()
    {
        if (string.IsNullOrEmpty(_bearer))
            throw new InvalidOperationException("Not authenticated. Call LoginWithOAuthAsync first.");

        var response = await $"{_baseUrl}api/me"
            .WithHeader("Accept", "application/json")
            .WithHeader("Authorization", $"Bearer {_bearer}")
            //.WithCookies(_cookieContainer)
            .GetJsonAsync<Dictionary<string, object>>();

        return response;
    }

    private async Task<string> PerformOAuthLoginAsync(OAuthConfig config)
    {
        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            throw new InvalidOperationException("Missing credentials");

        var authorizeUrl = BuildAuthorizeUrl(config);
        var session = await InitiateAuthFlowAsync(authorizeUrl, config);
        
        if (!string.IsNullOrEmpty(session.State))
            config.State = session.State;

        var nextUrl = await SubmitCredentialsAsync(session, config);
        var code = await FollowAuthorizationRedirectsAsync(nextUrl, config);
        var token = await ExchangeCodeForTokenAsync(code, config);

        return token;
    }

    private async Task<AuthSession> InitiateAuthFlowAsync(string authorizeUrl, OAuthConfig config)
    {
        string loginUrl = null;

        var response = await authorizeUrl
            .WithAutoRedirect(true)
            .WithCookies(out _cookieContainer)
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
        var csrfCookie = _cookieContainer.FirstOrDefault(c => c.OriginUrl.Root == authUrl
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

    private async Task<string> SubmitCredentialsAsync(AuthSession session, OAuthConfig config)
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
            ["username"] = _username,
            ["password"] = _password,
            ["connection"] = "pelo-user-password",
            ["code_challenge"] = config.CodeChallenge,
            ["code_challenge_method"] = config.CodeChallengeMethod
        };

        var loginEndpoint = $"https://{config.AuthDomain}/usernamepassword/login";
        
        var response = await loginEndpoint
            .WithCookies(_cookieContainer)
            .WithHeader("Content-Type", "application/json")
            .WithHeader("Accept", "*/*")
            .WithHeader("Origin", $"https://{config.AuthDomain}")
            .WithHeader("Referer", session.LoginUrl)
            .WithHeader("Auth0-Client", Auth0ClientPayload)
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

        return await SubmitHiddenFormAsync(actionNext, hiddenFields);
    }

    private async Task<string> SubmitHiddenFormAsync(string action, Dictionary<string, string> fields)
    {
        var actionUrl = action;
        if (!actionUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            actionUrl = EnsureAbsoluteUrl(AuthDomain, action);
        }

        var response = await actionUrl
            .WithCookies(_cookieContainer)
            .WithHeader("Content-Type", "application/x-www-form-urlencoded")
            .WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:145.0) Gecko/20100101 Firefox/145.0")
            .WithAutoRedirect(false)
            .AllowAnyHttpStatus()
            .PostUrlEncodedAsync(fields);

        var location = response.Headers.FirstOrDefault("Location");
        if (!string.IsNullOrEmpty(location))
        {
            return EnsureAbsoluteUrl(AuthDomain, location);
        }

        return response.ResponseMessage.RequestMessage?.RequestUri?.ToString() ?? actionUrl;
    }

    private async Task<string> FollowAuthorizationRedirectsAsync(string startUrl, OAuthConfig config)
    {
        string callbackUrl = null;
        string code = null;
        string state = null;

        var response = await startUrl
            .WithCookies(_cookieContainer)
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

    private async Task<string> ExchangeCodeForTokenAsync(string code, OAuthConfig config)
    {
        var endpoint = $"https://{config.AuthDomain}{AuthTokenPath}";
        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = config.ClientId,
            ["code_verifier"] = config.CodeVerifier,
            ["code"] = code,
            ["redirect_uri"] = config.RedirectUri
        };

        var response = await endpoint
            .WithCookies(_cookieContainer)
            .WithHeader("Content-Type", "application/json")
            .WithHeader("Accept", "application/json")
            .AllowAnyHttpStatus()
            .PostJsonAsync(payload);

        if (response.StatusCode >= 400)
        {
            var errorBody = await response.GetStringAsync();
            throw new InvalidOperationException(
                $"Token exchange failed: {Truncate(errorBody, AuthRedirectLogLimit)}");
        }

        var tokenResponse = await response.GetJsonAsync<TokenResponse>();
        
        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            throw new InvalidOperationException("Token exchange response missing access token");

        return tokenResponse.AccessToken;
    }

    private static OAuthConfig GenerateOAuthConfig()
    {
        var verifier = GenerateRandomString(64);
        var challenge = GenerateCodeChallenge(verifier);
        var state = GenerateRandomString(32);
        var nonce = GenerateRandomString(32);

        return new OAuthConfig
        {
            ClientId = AuthClientId,
            CodeVerifier = verifier,
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            State = state,
            Nonce = nonce,
            RedirectUri = AuthRedirectUri,
            Audience = AuthAudience,
            Scope = AuthScope,
            AuthDomain = AuthDomain
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

    private static string BuildAuthorizeUrl(OAuthConfig config)
    {
        return $"https://{config.AuthDomain}{AuthAuthorizePath}"
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
            .SetQueryParam("auth0Client", Auth0ClientPayload);
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
                    fields[name] = value;
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
    }

    private class AuthSession
    {
        public string LoginUrl { get; set; }
        public string State { get; set; }
        public string CsrfToken { get; set; }
    }

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}      