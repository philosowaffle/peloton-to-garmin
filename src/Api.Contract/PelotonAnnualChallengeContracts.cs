namespace Api.Contract;

public record ProgressGetResponse
{
	public ProgressGetResponse()
	{
		Tiers = new List<Tier>();
	}

	public double EarnedMinutes { get; init; }
	public ICollection<Tier> Tiers { get; init; }
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
	public Tier() { }

	public string? BadgeUrl { get; init; }
	public string? Title { get; init; }
	public double RequiredMinutes { get; init; }
	public bool HasEarned { get; init; }
	public float PercentComplete { get; init; }
	public bool IsOnTrackToEarndByEndOfYear { get; init; }
	public double MinutesBehindPace { get; init; }
	public double MinutesAheadOfPace { get; init; }
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