using Common.Dto;
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
		try
		{
			var serviceResult = await _annualChallengeService.GetAnnualChallengeProgressAsync(userId);

			if (serviceResult.IsErrored())
				return serviceResult.GetResultForError();

			var data = serviceResult.Result;
			var tiers = data.Tiers?.Select(t => new Common.Dto.Api.Tier()
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
			}).ToList();

			return Ok(new ProgressGetResponse()
			{
				EarnedMinutes = data.EarnedMinutes,
				Tiers = tiers ?? new List<Common.Dto.Api.Tier>(),
			});
		}
		catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}
}
