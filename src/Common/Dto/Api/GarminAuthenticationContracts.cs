namespace Common.Dto.Api;

public record GarminAuthenticationMfaTokenPostRequest
{
	public string MfaToken { get; set; }
}

public record GarminAuthenticationGetResponse
{
	public bool IsAuthenticated { get; init; }
}
