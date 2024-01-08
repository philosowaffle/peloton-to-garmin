using Api.Contract;
using Api.Service;
using Api.Service.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
public class PelotonAnnualChallengeController : Controller
{
	private readonly IPelotonAnnualChallengeService _annualChallengeService;

	public PelotonAnnualChallengeController(IPelotonAnnualChallengeService annualChallengeService)
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
		try
		{
			var serviceResult = await _annualChallengeService.GetProgressAsync();

			if (serviceResult.IsErrored())
				return serviceResult.GetResultForError();

			return Ok(serviceResult.Result);
		}
		catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}
}
