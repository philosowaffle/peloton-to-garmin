using System.Collections.Generic;

namespace Peloton.AnnualChallenge;

public record AnnualChallengeProgress
{
	public bool HasJoined { get; set; }
	public double EarnedMinutes { get; set; }
	public ICollection<Tier> Tiers { get; set; }
}

public record Tier
{
	public string BadgeUrl { get; set; }
	public string Title { get; set; }
	public double RequiredMinutes { get; set; }
	public bool HasEarned { get; set; }
	public double PercentComplete { get; set; }
	public bool IsOnTrackToEarndByEndOfYear { get; set; }
	public double MinutesBehindPace { get; set; }
	public double MinutesAheadOfPace { get; set; }
	public double MinutesNeededPerDay { get; set; }
	public double MinutesNeededPerWeek { get; set; }
}
