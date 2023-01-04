using System.Collections.Generic;

namespace Common.Dto.Api;

public record ProgressGetResponse
{
	public short EarnedMinutes { get; set; }
	public ICollection<Tier> Tiers { get; set; }
}

public record Tier
{
	public string BadgeUrl { get; set; } // tiers.detailed_badge_image
	public string Title { get; set; }
	public short RequiredMinutes { get; set; }
	public bool HasEarned { get; set; }
	public bool IsOnTrackToEarndByEndOfYear { get; set; }
	public bool MinutesBehindPace { get; set; }
	public bool MinutesAheadOfPace { get; set; }
}