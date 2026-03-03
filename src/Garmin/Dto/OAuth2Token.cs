using System;

namespace Garmin.Dto;

public record OAuth2Token
{
	public string Scope { get; set; }
	public string Jti { get; set; }
	public string Access_Token { get; set; }
	public string Token_Type { get; set; }
	public string Refresh_Token { get; set; }
	public int Expires_In { get; set; }
	public DateTime ExpiresAt { get; set; }
	public int Refresh_Token_Expires_In { get; set; }

	public bool IsExpired()
	{
		return ExpiresAt < DateTime.Now.AddHours(1); // pad the time a bit
	}
}
