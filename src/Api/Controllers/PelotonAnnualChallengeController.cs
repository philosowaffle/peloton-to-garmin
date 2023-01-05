using Common.Dto.Api;
using Microsoft.AspNetCore.Mvc;
using Peloton.AnnualChallenge;

namespace Api.Controllers;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class PelotonAnnualChallengeController : Controller
{
	private readonly IAnnualChallengeService _annualChallengeService;

	public PelotonAnnualChallengeController(IAnnualChallengeService annualChallengeService)
	{
		_annualChallengeService = annualChallengeService;
	}

	/// <summary>
	/// Fetches a progress summary for the Peloton Annual Challenge.
	/// </summary>
	/// <response code="200">Returns the progress summary</response>
	/// <response code="400">Invalid request values.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpGet]
	[Route("api/pelotonannualchallenge/progress")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<ProgressGetResponse>> GetProgressSummaryAsync()
	{
		var userId = 1;
		var result = await _annualChallengeService.GetAnnualChallengeProgressAsync(userId);

		return null;
	}
}
