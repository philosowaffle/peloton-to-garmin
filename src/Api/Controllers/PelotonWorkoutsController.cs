using Common.Dto.Api;
using Common.Dto.Peloton;
using Microsoft.AspNetCore.Mvc;
using Peloton;
using Peloton.Dto;

namespace Api.Controllers
{
	[ApiController]
	[Route("api/peloton/workouts")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class PelotonWorkoutsController : Controller
	{
		private readonly IPelotonService _pelotonService;

		public PelotonWorkoutsController(IPelotonService pelotonService)
		{
			_pelotonService = pelotonService;
		}

		/// <summary>
		/// Fetches a paged list of workouts recorded on Peloton ordered by Most Recent first.
		/// </summary>
		/// <response code="200">Returns the list of recent peloton workouts.</response>
		/// <response code="400">Invalid request values.</response>
		/// <response code="500">Unhandled exception.</response>
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<PelotonWorkoutsGetResponse>> GetAsync([FromQuery]PelotonWorkoutsGetRequest request)
		{
			if (request.PageSize <= 0)
				return BadRequest(new ErrorResponse("PageSize must be greater than 0."));
			if (request.PageIndex < 0)
				return BadRequest(new ErrorResponse("PageIndex must be greater than or equal to 0."));

			RecentWorkouts? recentWorkouts = null;

			try
			{
				recentWorkouts = await _pelotonService.GetPelotonWorkoutsAsync(request.PageSize, request.PageIndex);
			} 
			catch (ArgumentException ae) 
			{
				return BadRequest(new ErrorResponse(ae.Message));
			}
			catch (PelotonAuthenticationError pe) 
			{
				return BadRequest(new ErrorResponse(pe.Message));
			} catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}

			return new PelotonWorkoutsGetResponse() 
			{ 
				PageSize = recentWorkouts.Limit,
				PageIndex = recentWorkouts.Page,
				PageCount = recentWorkouts.Page_Count,
				TotalItems = recentWorkouts.Total,
				Items = recentWorkouts.data.OrderByDescending(i => i.Created_At).ToList() 
			};
		}
	}
}
