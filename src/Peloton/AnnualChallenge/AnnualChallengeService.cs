using Common.Dto;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Peloton.AnnualChallenge;

public interface IAnnualChallengeService
{
	Task<ServiceResult<AnnualChallengeProgress>> GetAnnualChallengeProgressAsync(int userId);
}

public class AnnualChallengeService : IAnnualChallengeService
{
	private const string AnnualChallengeId = "fa65351309d6476a8aadcc53902d78ec";

	private IPelotonApi _pelotonApi;

	public AnnualChallengeService(IPelotonApi pelotonApi)
	{
		_pelotonApi = pelotonApi;
	}

	public async Task<ServiceResult<AnnualChallengeProgress>> GetAnnualChallengeProgressAsync(int userId)
	{
		var result = new ServiceResult<AnnualChallengeProgress>();
		result.Result = new AnnualChallengeProgress();

		var joinedChallenges = await _pelotonApi.GetJoinedChallengesAsync(userId);
		if (joinedChallenges == null || joinedChallenges.Challenges.Length <= 0)
			return result;

		var annualChallenge = joinedChallenges.Challenges.FirstOrDefault(c => c.Challenge_Summary.Id == AnnualChallengeId || c.Challenge_Summary.Title.StartsWith("The Annual"));
		if (annualChallenge is null)
			return result;

		var annualChallengeProgressDetail = await _pelotonApi.GetUserChallengeDetailsAsync(userId, AnnualChallengeId);
		if (annualChallengeProgressDetail is null)
			return result;

		var tiers = annualChallengeProgressDetail.Challenge_Detail.Tiers;
		var progress = annualChallengeProgressDetail.Progress;

		var now = DateTime.UtcNow;
		var startTimeUtc = DateTimeOffset.FromUnixTimeSeconds(annualChallengeProgressDetail.Challenge_Summary.Start_Time).UtcDateTime;
		var endTimeUtc = DateTimeOffset.FromUnixTimeSeconds(annualChallengeProgressDetail.Challenge_Summary.End_Time).UtcDateTime;
		var elapsedTime = now - startTimeUtc;
		var elapsedDays = Math.Ceiling(elapsedTime.TotalDays);

		result.Result.HasJoined = true;
		result.Result.EarnedMinutes = progress.Metric_Value;
		result.Result.CurrentDailyPace = progress.Metric_Value / elapsedDays;
		result.Result.CurrentWeeklyPace = result.Result.CurrentDailyPace * 7;
		result.Result.Tiers = tiers.Where(t => t.Metric_Value > 0).Select(t => 
		{
			var requiredMinutes = t.Metric_Value;
			var actualMinutes = progress.Metric_Value;
			var onTrackDetails = CalculateOnTrackDetails(now, startTimeUtc, endTimeUtc, actualMinutes, requiredMinutes);

			return new Tier()
			{
				BadgeUrl = t.detailed_badge_image_url,
				Title = t.Title,
				RequiredMinutes = requiredMinutes,
				HasEarned = onTrackDetails.HasEarned,
				PercentComplete= onTrackDetails.PercentComplete,
				IsOnTrackToEarndByEndOfYear = onTrackDetails.IsOnTrackToEarnByEndOfYear,
				MinutesBehindPace = onTrackDetails.MinutesBehindPace,
				MinutesAheadOfPace = onTrackDetails.MinutesBehindPace * -1,
				MinutesNeededPerDay = onTrackDetails.MinutesNeededPerDay,
				MinutesNeededPerWeek = onTrackDetails.MinutesNeededPerDay * 7,
				MinutesNeededPerDayToFinishOnTime = onTrackDetails.MinutesNeededPerDayToFinishOnTime,
				MinutesNeededPerWeekToFinishOnTime = onTrackDetails.MinutesNeededPerDayToFinishOnTime * 7
			};
		}).ToList();

		return result;
	}

	public static OnTrackDetails CalculateOnTrackDetails(DateTime now, DateTime startTimeUtc, DateTime endTimeUtc, double earnedMinutes, double requiredMinutes)
	{
		var totalTime = endTimeUtc - startTimeUtc;
		var totalDays = Math.Ceiling(totalTime.TotalDays);

		var minutesNeededPerDay = requiredMinutes / totalDays;

		var elapsedTime = now - startTimeUtc;
		var elapsedDays = Math.Ceiling(elapsedTime.TotalDays);

		var neededMinutesToBeOnTrack = elapsedDays * minutesNeededPerDay;

		var remainingDays = Math.Ceiling((endTimeUtc - now).TotalDays);
		var minutesNeededPerDayToFinishOnTime = (requiredMinutes - earnedMinutes) / remainingDays;

		return new OnTrackDetails()
		{
			IsOnTrackToEarnByEndOfYear = earnedMinutes >= neededMinutesToBeOnTrack,
			MinutesBehindPace = neededMinutesToBeOnTrack - earnedMinutes,
			MinutesNeededPerDay = minutesNeededPerDay,
			HasEarned = earnedMinutes >= requiredMinutes,
			PercentComplete = earnedMinutes / requiredMinutes,
			MinutesNeededPerDayToFinishOnTime = minutesNeededPerDayToFinishOnTime
		};
	}

	public record OnTrackDetails
	{
		public bool IsOnTrackToEarnByEndOfYear { get; init; }
		public double MinutesBehindPace { get; init; }
		public double MinutesNeededPerDay { get; init; }
		public bool HasEarned { get; init; }
		public double PercentComplete { get; init; }
		public double MinutesNeededPerDayToFinishOnTime { get; init; }
	}
}
