namespace Garmin.Dto;

public record DITokenSlot
{
	public string ClientId { get; set; }
	public OAuth2Token Token { get; set; }
}
