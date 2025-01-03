using System;
using System.Linq;
using System.Collections.Generic;
using Serilog;
using Common.Observe;

namespace Garmin
{
    public class GarminOAuthV1
    {
        private static readonly ILogger _logger = LogContext.ForClass<GarminOAuthV1>();
        
        private const string OAUTH_VERSION = "1.0";
        private const string OAUTH_SIGNATURE_METHOD = "HMAC-SHA1";

        private Dictionary<String,String> oauth_params;
        private string consumerSecret;
        private string tokenSecret;
        private string normalized;
        private string method;
        private string signatureBase;
        private string requestUri;

        private GarminOAuthV1()
        {
            oauth_params = new Dictionary<String,String>()
            {
                {"oauth_version", OAUTH_VERSION},
                {"oauth_signature_method", OAUTH_SIGNATURE_METHOD}
            };
        }
        public static GarminOAuthV1 ForRequestToken(String requestUri, String consumerKey, String consumerSecret)
        {
            var oauth = new GarminOAuthV1();
            oauth.method = "GET";
            oauth.requestUri = requestUri;
            oauth.consumerSecret = consumerSecret;
            oauth.oauth_params["oauth_consumer_key"] = consumerKey;
            return oauth;
        }
        
        public static GarminOAuthV1 ForProtectedResource(String method,
                                                         String requestUri,
                                                         String consumerKey,
                                                         String consumerSecret,
                                                         String token,
                                                         String tokenSecret)
        {
            var oauth = new GarminOAuthV1();
            oauth.method = method.ToUpper();
            oauth.requestUri = requestUri;
            oauth.oauth_params["oauth_consumer_key"] = consumerKey;
            oauth.consumerSecret = consumerSecret;
            oauth.oauth_params["oauth_token"] = token;
            oauth.tokenSecret = tokenSecret;
            return oauth;
        }

        private string Nonce()
        {
            // 30 random decimal digits
            return string.Join("", Enumerable.Range(48, 10) 
                       .SelectMany(x => Enumerable.Repeat(x, 12)) 
                       .OrderBy(x => Guid.NewGuid()) 
                       .Take(30) 
                       .Select(x => (char)x)); 
        }

        private String Normalize(Dictionary<String,String> extraParameters)
        {
            var paramsToSign =
                oauth_params.Concat(extraParameters.Where(kvp => !oauth_params.ContainsKey(kvp.Key)))
                    .ToDictionary(kvp=> kvp.Key, kvp => kvp.Value);
            var sb = new System.Text.StringBuilder();

            List<string> keyList = new List<string>(paramsToSign.Keys);
            keyList = keyList.OrderBy(k => k).ToList(); // required
            
            foreach(var key in keyList) {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.AppendFormat("{0}={1}", key, System.Net.WebUtility.UrlEncode(paramsToSign[key]));
            }
            normalized = sb.ToString();
            return normalized;
        }
        
        public String GetAuthzHeader(Dictionary<String,String> extraParameters)
        {
            oauth_params["oauth_nonce"] = Nonce();
            oauth_params["oauth_timestamp"] = String.Format("{0}",DateTimeOffset.Now.ToUnixTimeSeconds());
            Normalize(extraParameters);
            _logger.Information("normalized parameters: {0}", normalized);
            signatureBase = String.Format("{0}&{1}&{2}",
                this.method, System.Net.WebUtility.UrlEncode(this.requestUri),
                System.Net.WebUtility.UrlEncode(this.normalized));
            _logger.Information("signature base: {0}", signatureBase);

            // get the secret key
            var secretKey = consumerSecret + "&";
            if (!String.IsNullOrEmpty(tokenSecret)) {
                secretKey += tokenSecret;
            }

            // produce the signature
            var hmac = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes(secretKey));
            var signature = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureBase));
            var encodedSignature = System.Convert.ToBase64String(signature);
            _logger.Information("signature: {0}",  encodedSignature);
            oauth_params.Add("oauth_signature", encodedSignature);

            // produce the header containing the OAuth params
            var sb = new System.Text.StringBuilder();
            List<string> keyList = new List<string>(oauth_params.Keys);
            keyList = keyList.OrderBy(k => k).ToList(); // unnecessary, but kind

            foreach (var key in keyList)
            {
                sb.Append((sb.Length > 0) ? ", " : "OAuth ");
                sb.AppendFormat("{0}=\"{1}\"", key, System.Net.WebUtility.UrlEncode(oauth_params[key]));
            }
            var authz_header = sb.ToString();
            return authz_header;
        }
    }
}
