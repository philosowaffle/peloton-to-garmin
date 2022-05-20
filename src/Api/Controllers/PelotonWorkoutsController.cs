using Api.Contracts;
using Common.Observe;
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
		/// Fetches recent workouts recorded on Peloton.
		/// </summary>
		/// <response code="200">Returns the list of recent peloton workouts.</response>
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<RecentWorkoutsGetResponse> GetAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonWorkoutsController)}.{nameof(GetAsync)}");

			var recentWorkouts = await _pelotonService.GetRecentWorkoutsAsync(25);

			return new RecentWorkoutsGetResponse() { Items = recentWorkouts.OrderByDescending(i => i.Created_At).ToList() };

		}
	}
}
