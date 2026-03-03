using Api.Contract;
using Api.Service.Helpers;
using Api.Service.Mappers;
using Common.Dto;
using Peloton.AnnualChallenge;

namespace Api.Service;

public interface IPelotonAnnualChallengeService
{
	Task<ServiceResult<ProgressGetResponse>> GetProgressAsync();
}

public class PelotonAnnualChallengeService : IPelotonAnnualChallengeService
{
	private readonly IAnnualChallengeService _service;

	public PelotonAnnualChallengeService(IAnnualChallengeService service)
	{
		_service = service;
	}

	public async Task<ServiceResult<ProgressGetResponse>> GetProgressAsync()
	{
		var userId = 1;
		var result = new ServiceResult<ProgressGetResponse>();

		try
		{
			var serviceResult = await _service.GetAnnualChallengeProgressAsync(userId);

			if (serviceResult.IsErrored())
			{
				result.Successful = serviceResult.Successful;
				result.Error = serviceResult.Error;
				return result;
			}

			var data = serviceResult.Result;
			var tiers = data.Tiers?.Select(t => t.Map()).ToList();

			result.Result = new ProgressGetResponse()
			{
				EarnedMinutes = data.EarnedMinutes,
				Tiers = tiers ?? new List<Contract.Tier>(),
				CurrentDailyPace = data.CurrentDailyPace,
				CurrentWeeklyPace = data.CurrentWeeklyPace,
			};

			return result;
		}
		catch (Exception e)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Exception = e, Message = "Failed to fetch Peloton Annual Challenge data. See logs for more details." };
			return result;
		}
	}
}
