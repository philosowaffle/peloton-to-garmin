using Common.Dto;
using System.Linq;
using System.Threading.Tasks;

namespace Peloton.AnnualChallenge;

public interface IAnnualChallengeService
{
	Task<ServiceResult<AnnualChallengeProgress>> GetAnnualChallengeProgressAsync(int userId);
}

public class AnnualChallengeService : IAnnualChallengeService
{
	private const string AnnualChallengeId = "66863eacd9d04447979d5dba7bf0e766";

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

		var annualChallenge = joinedChallenges.Challenges.FirstOrDefault(c => c.Id == AnnualChallengeId);
		if (annualChallenge is null)
			return result;

		var annualChallengeProgressDetail = await _pelotonApi.GetUserChallengeDetailsAsync(userId, AnnualChallengeId);
		if (annualChallengeProgressDetail is null)
			return result;

		// do stuff

		return result;
	}
}
