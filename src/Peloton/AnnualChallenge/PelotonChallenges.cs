namespace Peloton.AnnualChallenge;

public record PelotonChallenges
{
	public ChallengeSummary[] Challenges { get; set; }
}

public record PelotonChallenge
{
	public ChallengeSummary ChallengeSummary { get; }

	// participants
	// progress
}

public record ChallengeSummary
{
	public string Id { get; }
	public string Title { get; }
	public string Symbol_Image_Url { get; }
	public long Start_Time { get; }
	public long End_Time { get; }
}

public record PelotonUserChallengeDetail
{
	public ChallengeDetail Challenge_Detail { get; }
	public ChallengeSummary ChallengeSummary { get; }
}

public record ChallengeDetail
{
	public string Detailed_Description { get; }
	public PelotonChallengeTier[] Tiers { get; }

}

public record PelotonChallengeTier
{
	public string Id { get; }
	public string Title { get; }
	public string detailed_badge_image_url { get; }
	public short Metric_Value { get; set; }
	public string Metric_Display_Unit { get; set; }
}