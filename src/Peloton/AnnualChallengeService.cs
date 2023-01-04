using Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peloton;

public interface IAnnualChallengeService
{
	Task<ServiceResult<string>> GetAnnualChallengeProgressAsync(int userId);
}

public class AnnualChallengeService : IAnnualChallengeService
{
	public Task<ServiceResult<string>> GetAnnualChallengeProgressAsync(int userId)
	{
		throw new NotImplementedException();
	}
}
