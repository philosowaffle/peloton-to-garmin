namespace Garmin.Dto;

public class GarminApiAuthentication
{
	public AuthStage AuthStage { get; set; }
	public OAuth2Token OAuth2Token { get; set; }

	public bool IsValid()
	{
		return AuthStage == AuthStage.Completed
			&& OAuth2Token is object
			&& !OAuth2Token.IsExpired();
	}
}

public enum AuthStage : byte
{
	None = 0,
	NeedMfaToken = 1,
	Completed = 2,
}
