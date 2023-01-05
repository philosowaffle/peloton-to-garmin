using System.Collections.Generic;

namespace Peloton.AnnualChallenge;

public record AnnualChallengeProgress
{
	public bool HasJoined { get; set; }
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
	public short MinutesNeededPerDay { get; set; }
	public short MinutesNeededPerWeek { get; set; }
}
