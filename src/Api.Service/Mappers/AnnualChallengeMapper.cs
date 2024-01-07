using Api.Contract;

namespace Api.Service.Mappers;

public static class AnnualChallengeMapper
{
	public static Tier Map(this Peloton.AnnualChallenge.Tier t)
	{
		return new Tier()
		{
			BadgeUrl = t.BadgeUrl,
			Title = t.Title,
			RequiredMinutes = t.RequiredMinutes,
			HasEarned = t.HasEarned,
			PercentComplete = Convert.ToSingle(t.PercentComplete * 100),
			IsOnTrackToEarndByEndOfYear = t.IsOnTrackToEarndByEndOfYear,
			MinutesBehindPace = t.MinutesBehindPace,
			MinutesAheadOfPace = t.MinutesAheadOfPace,
			MinutesNeededPerDay = t.MinutesNeededPerDay,
			MinutesNeededPerWeek = t.MinutesNeededPerWeek,
			MinutesNeededPerDayToFinishOnTime = t.MinutesNeededPerDayToFinishOnTime,
			MinutesNeededPerWeekToFinishOnTime = t.MinutesNeededPerWeekToFinishOnTime
		};
	}
}
