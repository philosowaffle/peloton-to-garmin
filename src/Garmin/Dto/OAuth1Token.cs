namespace Garmin.Dto;

public record OAuth1Token
{
	public string Token { get; set; }
	public string TokenSecret { get; set; }
}
