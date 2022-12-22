namespace Common.Dto.P2G;

public record P2GUser
{
	public byte Id { get; init; }
	public string UserName { get; init; }
}