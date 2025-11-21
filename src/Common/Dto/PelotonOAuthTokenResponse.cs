using System;

namespace Common.Dto
{
	public class PelotonOAuthTokenResponse
	{
		public string access_token { get; set; }
		public string token_type { get; set; }
		public int expires_in { get; set; }
		public string refresh_token { get; set; }
		public string scope { get; set; }
		public string user_id { get; set; }
		public DateTime ExpiresAt { get; set; }

		public bool IsExpired()
		{
			// Add 1 hour buffer to ensure token doesn't expire during use
			return ExpiresAt < DateTime.UtcNow.AddHours(1);
		}

		public void SetExpiresAt()
		{
			ExpiresAt = DateTime.UtcNow.AddSeconds(expires_in);
		}
	}
}
