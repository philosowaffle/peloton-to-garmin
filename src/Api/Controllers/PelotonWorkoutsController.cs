using Common.Dto.Api;
using Microsoft.AspNetCore.Mvc;
using Peloton;

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
		/// Fetches workouts recorded on Peloton.
		/// </summary>
		/// <response code="200">Returns the list of recent peloton workouts.</response>
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<PelotonWorkoutsGetResponse>> GetAsync([FromQuery]PelotonWorkoutsGetRequest request)
		{
			if (request.PageSize <= 0)
				return BadRequest("PageSize must be greater than 0.");
			if (request.PageIndex < 0)
				return BadRequest("PageIndex must be greater than or equal to 0.");

			var recentWorkouts = await _pelotonService.GetPelotonWorkoutsAsync(request.PageSize, request.PageIndex);

			return Ok(new PelotonWorkoutsGetResponse() 
			{ 
				PageSize = recentWorkouts.Limit,
				PageIndex = recentWorkouts.Page,
				PageCount = recentWorkouts.Page_Count,
				TotalItems = recentWorkouts.Total,
				Items = recentWorkouts.data.OrderByDescending(i => i.Created_At).ToList() 
			});
		}
	}
}
