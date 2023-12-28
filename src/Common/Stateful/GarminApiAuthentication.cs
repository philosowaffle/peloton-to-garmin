using Common.Dto;
using Flurl.Http;

namespace Common.Stateful;

public class GarminApiAuthentication : IApiAuthentication
{
	public string Email { get; set; }
	public string Password { get; set; }
	public AuthStage AuthStage { get; set; }
	public CookieJar CookieJar { get; set; }
	public string UserAgent { get; set; } = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";
	public string MFACsrfToken { get; set; }
	public OAuth1Token OAuth1Token { get; set; }
	public OAuth2Token OAuth2Token { get; set; }

	public bool IsValid(Settings settings)
	{
		return Email == settings.Garmin.Email
			&& Password == settings.Garmin.Password
			&& AuthStage == AuthStage.Completed
			&& !string.IsNullOrWhiteSpace(OAuth2Token?.Access_Token);
	}
}

public class OAuth1Token
{
	public string Token { get; set; }
	public string TokenSecret { get; set; }
}

public enum AuthStage : byte
{
	None = 0,
	NeedMfaToken = 1,
	Completed = 2,
}
