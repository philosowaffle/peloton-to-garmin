using Common.Dto;
using System;

namespace Common.Stateful
{
	public class PelotonApiAuthentication
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string UserId { get; set; }
		public string SessionId { get; set; }
		public string BearerToken { get; set; }
		public PelotonOAuthTokenResponse OAuthToken { get; set; }

		public bool IsValid(Settings settings)
		{
			// Check OAuth token validity first
			if (OAuthToken != null && !OAuthToken.IsExpired())
			{
				return Email == settings.Peloton.Email
					&& Password == settings.Peloton.Password
					&& !string.IsNullOrEmpty(OAuthToken.user_id);
			}

			// Fallback to legacy session-based auth
			return Email == settings.Peloton.Email
				&& Password == settings.Peloton.Password
				&& !string.IsNullOrEmpty(UserId)
				&& !string.IsNullOrEmpty(SessionId);
		}

		public string GetAccessToken()
		{
			// Prefer OAuth token if available
			if (OAuthToken != null && !string.IsNullOrEmpty(OAuthToken.access_token))
			{
				return OAuthToken.access_token;
			}

			// Fallback to bearer token
			return BearerToken;
		}

		public string GetUserId()
		{
			// Prefer OAuth token user_id if available
			if (OAuthToken != null && !string.IsNullOrEmpty(OAuthToken.user_id))
			{
				return OAuthToken.user_id;
			}

			// Fallback to UserId
			return UserId;
		}
	}
}
