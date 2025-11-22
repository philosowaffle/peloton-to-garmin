using Common.Dto;
using System;

namespace Common.Stateful
{
	public class PelotonApiAuthentication
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string UserId { get; set; }
		public Token Token { get; set; }
		public DateTime ExpiresAt { get; set; }

		public bool IsValid(PelotonSettings settings)
		{
			return Email == settings.Email
				&& Password == settings.Password
				&& !string.IsNullOrEmpty(UserId)
				&& !string.IsNullOrEmpty(Token.AccessToken)
				&& ExpiresAt > DateTime.UtcNow;
		}
	}

	public class Token
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string IdToken { get; set; }

        public string Scope { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
