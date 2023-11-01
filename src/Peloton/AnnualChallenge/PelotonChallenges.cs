namespace Peloton.AnnualChallenge;

public record PelotonChallenges
{
	public PelotonChallenge[] Challenges { get; set; }
}

public record PelotonChallenge
{
	public ChallengeSummary Challenge_Summary { get; set; }

	// participants
	// progress
}

public record ChallengeSummary
{
	public string Id { get; set; }
	public string Title { get; set; }
	public string Symbol_Image_Url { get; set; }
	public long Start_Time { get; set; }
	public long End_Time { get; set; }
}

public record PelotonUserChallengeDetail
{
	public ChallengeDetail Challenge_Detail { get; set; }
	public ChallengeSummary Challenge_Summary { get; set; }
	public ChallengeProgress Progress { get; set; }
}

public record ChallengeDetail
{
	public string Detailed_Description { get; set; }
	public PelotonChallengeTier[] Tiers { get; set; }

}

public record PelotonChallengeTier
{
	public string Id { get; set; }
	public string Title { get; set; }
	public string detailed_badge_image_url { get; set; }
	public double Metric_Value { get; set; }
	public string Metric_Display_Unit { get; set; }
}

public record ChallengeProgress
{
	// Current User Value
	public double Metric_Value { get; set; }
	public string Metric_Display_Unit { get; set; }
}