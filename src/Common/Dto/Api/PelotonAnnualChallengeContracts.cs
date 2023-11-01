using System.Collections.Generic;

namespace Common.Dto.Api;

public record ProgressGetResponse
{
	public double EarnedMinutes { get; init; }
	public ICollection<Tier> Tiers { get; init; }
}

public record Tier
{
	public string BadgeUrl { get; init; }
	public string Title { get; init; }
	public double RequiredMinutes { get; init; }
	public bool HasEarned { get; init; }
	public float PercentComplete { get; init; }
	public bool IsOnTrackToEarndByEndOfYear { get; init; }
	public double MinutesBehindPace { get; init; }
	public double MinutesAheadOfPace { get; init; }
	public double MinutesNeededPerDay { get; init; }
	public double MinutesNeededPerWeek { get; init; }
}