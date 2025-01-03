using Common.Http;
using Common.Observe;
using Common.Extensions;
using Flurl.Http;
using Garmin.Auth;
using Garmin.Dto;
using Serilog;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Garmin
{
    public interface IGarminApiClient
    {
        Task InitCookieJarAsync(object queryParams, string userAgent, out CookieJar jar);
        Task<GarminResult> GetCsrfTokenAsync(Dictionary<String,String> queryParams, Dictionary<String,String> priorQueryParams, CookieJar jar, string userAgent);
        Task<SendCredentialsResult> SendCredentialsAsync(Dictionary<String,String> queryParams, Dictionary<String,String> payload, CookieJar jar, string userAgent);
        Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar);
        Task<string> GetOAuth1TokenAsync(ConsumerCredentials credentials, string ticket, string userAgent);
        Task<OAuth2Token> GetOAuth2TokenAsync(OAuth1Token oAuth1Token, ConsumerCredentials credentials, string userAgent);
        Task<ConsumerCredentials> GetConsumerCredentialsAsync();
        Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth, string userAgent);
    }

    public class ApiClient : IGarminApiClient
    {
        private const string SSO_SIGNIN_URL = "https://sso.garmin.com/sso/signin";
        private const string SSO_EMBED_URL = "https://sso.garmin.com/sso/embed";
        private const string UPLOAD_URL = "https://connectapi.garmin.com/upload-service/upload";
        private const string ORIGIN = "https://sso.garmin.com";
        private const string REFERER = "https://sso.garmin.com/sso/signin";

        private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();

        public Task<ConsumerCredentials> GetConsumerCredentialsAsync()
        {
            return "https://thegarth.s3.amazonaws.com/oauth_consumer.json"
                .GetJsonAsync<ConsumerCredentials>();
        }

        public Task InitCookieJarAsync(object queryParams, string userAgent, out CookieJar jar)
        {
            return SSO_EMBED_URL
                        .WithHeader("User-Agent", userAgent)
                        .WithHeader("origin", ORIGIN)
                        .SetQueryParams(queryParams)
                        .WithCookies(out jar)
                        .GetStringAsync();
        }

        public async Task<SendCredentialsResult> SendCredentialsAsync(Dictionary<String,String> queryParams, Dictionary<String,String> payload, CookieJar jar, string userAgent)
        {
            var referer = GetSigninReferer(queryParams);
            _logger.Information(String.Format("Using referer for Signin request: {0}", referer));
            var result = new SendCredentialsResult();
            result.RawResponseBody = await SSO_SIGNIN_URL
                        .WithHeader("User-Agent", userAgent)
                        .WithHeader("origin", ORIGIN)
                        .WithHeader("referer",  referer)
                        //.WithHeader("NK", "NT")
                        .SetQueryParams(queryParams)
                        .WithCookies(jar)
                        .StripSensitiveDataFromLogging(payload["username"], payload["password"])
                        .OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
                        .PostUrlEncodedAsync(payload)
                        .ReceiveString();

            return result;
        }


        private static string GetSigninReferer(Dictionary<String,String> source) {
            return String.Format("{0}?{1}", SSO_SIGNIN_URL,
                DictionaryToQueryString(source));
        }
        
        private static string GetEmbedReferer(Dictionary<String,String> source) {
            return String.Format("{0}?{1}", SSO_EMBED_URL,
                DictionaryToQueryString(source));
        }
        
        private static string DictionaryToQueryString(Dictionary<String,String> source) {
            if (source == null || source.Count == 0)
            {
                return string.Empty;
            }

            var queryString = new System.Text.StringBuilder();
            foreach (var item in source)
            {
                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }

                string parameterName = item.Key.ToString();
                string parameterValue = System.Net.WebUtility.UrlEncode(item.Value.ToString());
                queryString.AppendFormat("{0}={1}", parameterName, parameterValue);
            }

            return queryString.ToString();
        }
        
        public async Task<GarminResult> GetCsrfTokenAsync(Dictionary<String,String> queryParams, Dictionary<String,String> priorQueryParams, CookieJar jar, string userAgent)
        {
            var referer = GetEmbedReferer(priorQueryParams);
            _logger.Information(String.Format("Using referer for CSRF request: {0}", referer));
            var result = new GarminResult();
            result.RawResponseBody = await SSO_SIGNIN_URL
                        .WithHeader("User-Agent", userAgent)
                        .WithHeader("Referer", referer)
                        //.WithHeader("origin", ORIGIN)
                        .SetQueryParams(queryParams)
                        .WithCookies(jar)
                        .GetAsync()
                        .ReceiveString();
            return result;
        }

        public Task<string> SendMfaCodeAsync(string userAgent, object queryParams, object mfaData, CookieJar jar)
        {
            return "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"
                        .WithHeader("User-Agent", userAgent)
                        .WithHeader("origin", ORIGIN)
                        .SetQueryParams(queryParams)
                        .WithCookies(jar)
                        .OnRedirect(redir => redir.Request.WithCookies(jar))
                        .PostUrlEncodedAsync(mfaData)
                        .ReceiveString();
        }

        public Task<string> GetOAuth1TokenAsync(ConsumerCredentials credentials, string ticket, string userAgent)
        {
            var baseUri = "https://connectapi.garmin.com/oauth-service/oauth/preauthorized";
            var oauth = GarminOAuthV1.ForRequestToken(baseUri, credentials.Consumer_Key, credentials.Consumer_Secret);

            // these query params must be included in the signature base
            var queryParams = new Dictionary<String,String>() {
                {"ticket", ticket},
                {"login-url", SSO_EMBED_URL},
                {"accepts-mfa-tokens", "true"},
            };
            var authzHeader = oauth.GetAuthzHeader(queryParams);
            _logger.Information("Authz header {authz}", authzHeader);
            
            var requestUri = String.Format("{0}?{1}",baseUri,
                DictionaryToQueryString(queryParams));
            
            return requestUri
                            .WithHeader("Accept", "*/*")
                            .WithHeader("User-Agent", Defaults.UserAgent_ConnectMobile)
                            .WithHeader("Authorization", authzHeader)
                            .GetStringAsync();
        }
        
        public Task<OAuth2Token> GetOAuth2TokenAsync(OAuth1Token oAuth1Token, ConsumerCredentials credentials, string userAgent)
        {
            var exchangeUri = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";
            var oauth = GarminOAuthV1.ForProtectedResource("POST", exchangeUri, credentials.Consumer_Key, credentials.Consumer_Secret, 
                oAuth1Token.Token, oAuth1Token.TokenSecret);
            var authzHeader = oauth.GetAuthzHeader(new Dictionary<String,String>());
            _logger.Information("Authz header {authz}", authzHeader);
            return exchangeUri
                            .WithHeader("User-Agent", Defaults.UserAgent_ConnectMobile)
                                .WithHeader("Authorization", authzHeader)
                                .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                                .PostUrlEncodedAsync(new object()) // hack: PostAsync() will drop the content-type header, by posting empty object we trick flurl into leaving the header
                                .ReceiveJson<OAuth2Token>();
        }

        public async Task<UploadResponse> UploadActivity(string filePath, string format, GarminApiAuthentication auth, string userAgent)
        {
            _logger.Information("Upload activity using access token {token}...", auth.OAuth2Token.Access_Token.Truncate(32));
            var fileName = Path.GetFileName(filePath);
            var response = await $"{UPLOAD_URL}/{format}"
                .WithOAuthBearerToken(auth.OAuth2Token.Access_Token)
                //.WithHeader("NK", "NT")
                //.WithHeader("origin", ORIGIN)
                //.WithHeader("User-Agent", userAgent)
                .WithHeader("User-Agent", Defaults.UserAgent_ConnectMobile)
                .AllowHttpStatus("2xx,409")
                .PostMultipartAsync((data) =>
                {
                    data.AddFile("\"file\"", path: filePath, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");
                })
                .ReceiveJson<UploadResponse>();

            var result = response.DetailedImportResult;

            if (result.Failures.Any())
            {
                foreach (var failure in result.Failures)
                {
                    if (failure.Messages.Any())
                    {
                        foreach (var message in failure.Messages)
                        {
                            if (message.Code == 202)
                            {
                                _logger.Information("Activity already uploaded {garminWorkout}", result.FileName);
                            }
                            else
                            {
                                _logger.Error("Failed to upload activity to Garmin. Message: {errorMessage}", message);
                            }
                        }
                    }
                }
            }

            return response;
        }
    }
}
