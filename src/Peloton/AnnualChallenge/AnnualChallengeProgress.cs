using System.Collections.Generic;

namespace Peloton.AnnualChallenge;

public record AnnualChallengeProgress
{
	public bool HasJoined { get; set; }
	public double EarnedMinutes { get; set; }
	public ICollection<Tier> Tiers { get; set; }

	/// <summary>
	/// The average pace achieved so far in terms of minutes/day.
	/// </summary>
	public double CurrentDailyPace { get; set; }
	/// <summary>
	/// The average pace achieved so far in terms of minutes/week.
	/// </summary>
	public double CurrentWeeklyPace { get; set; }
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

	/// <summary>
	/// Assuming working evenly throughout the whole year, this is the amount of time to plan to spend per day.
	/// </summary>
	public double MinutesNeededPerDay { get; set; }
	/// <summary>
	/// Assuming working evenly throughout the whole year, this is the amount of time to plan to spend per week.
	/// </summary>
	public double MinutesNeededPerWeek { get; set; }
	/// <summary>
	/// Assuming working evenly throughout the remainder of the year, this is the amount of time to plan to spend per day. 
	/// </summary>
	public double MinutesNeededPerDayToFinishOnTime { get; set; }
	/// <summary>
	/// Assuming working evenly throughout the remainder of the year, this is the amount of time to plan to spend per week.
	/// </summary>
	public double MinutesNeededPerWeekToFinishOnTime { get; set; }
}
