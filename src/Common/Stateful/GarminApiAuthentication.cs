using Flurl.Http;

namespace Common.Stateful;

public class GarminApiAuthentication : IApiAuthentication
{
	public string Email { get; set; }
	public string Password { get; set; }
	public AuthStage AuthStage { get; set; }
	public CookieJar CookieJar { get; set; }
	public string UserAgent { get; set; } = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:48.0) Gecko/20100101 Firefox/50.0";
	public string MFACsrfToken { get; set; }

	public bool IsValid(Settings settings)
	{
		return Email == settings.Garmin.Email
			&& Password == settings.Garmin.Password
			&& CookieJar is object
			&& AuthStage == AuthStage.Completed;
	}
}

public enum AuthStage : byte
{
	None = 0,
	NeedMfaToken = 1,
	Completed = 2,
}
