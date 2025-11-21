using System;

namespace Common.Dto
{
	public class PelotonOAuthConfig
	{
		public string CodeVerifier { get; set; }
		public string CodeChallenge { get; set; }
		public string State { get; set; }
		public string Nonce { get; set; }
		public string RedirectUri { get; set; } = "https://members.onepeloton.com/callback";
		public string ClientId { get; set; } = "WVoJxVDdPoFx4RNewvvg6ch2mZ7bwnsM";
		public string AuthorizationEndpoint { get; set; } = "https://auth.onepeloton.com/authorize";
		public string TokenEndpoint { get; set; } = "https://auth.onepeloton.com/oauth/token";
		public string LoginEndpoint { get; set; } = "https://auth.onepeloton.com/usernamepassword/login";
		public string Audience { get; set; } = "https://api.onepeloton.com/";
		public string Scope { get; set; } = "offline_access openid peloton-api.members:default";

		public static PelotonOAuthConfig Generate()
		{
			var config = new PelotonOAuthConfig();

			// Generate code verifier (43-128 characters, random string)
			config.CodeVerifier = GenerateCodeVerifier();

			// Generate code challenge (SHA256 hash of verifier, base64url encoded)
			config.CodeChallenge = GenerateCodeChallenge(config.CodeVerifier);

			// Generate state and nonce for CSRF protection
			config.State = GenerateRandomString(32);
			config.Nonce = GenerateRandomString(32);

			return config;
		}

		private static string GenerateCodeVerifier()
		{
			// Generate 128 character random string using URL-safe characters
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
			var random = new Random();
			var result = new char[128];
			for (int i = 0; i < 128; i++)
			{
				result[i] = chars[random.Next(chars.Length)];
			}
			return new string(result);
		}

		private static string GenerateCodeChallenge(string verifier)
		{
			using (var sha256 = System.Security.Cryptography.SHA256.Create())
			{
				var hash = sha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(verifier));
				return Base64UrlEncode(hash);
			}
		}

		private static string GenerateRandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var random = new Random();
			var result = new char[length];
			for (int i = 0; i < length; i++)
			{
				result[i] = chars[random.Next(chars.Length)];
			}
			return new string(result);
		}

		private static string Base64UrlEncode(byte[] input)
		{
			var base64 = Convert.ToBase64String(input);
			// Convert to base64url encoding
			return base64
				.Replace('+', '-')
				.Replace('/', '_')
				.TrimEnd('=');
		}
	}
}
