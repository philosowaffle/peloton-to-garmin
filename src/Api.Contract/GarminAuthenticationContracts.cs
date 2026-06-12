namespace Api.Contract;

public record GarminAuthenticationMfaTokenPostRequest
{
	public string? MfaToken { get; set; }
}

public record GarminAuthenticationGetResponse
{
	public bool IsAuthenticated { get; init; }
}

public record GarminAuthenticationServiceTicketPostRequest
{
	public string? ServiceTicket { get; set; }
}
