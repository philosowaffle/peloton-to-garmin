using Api.Contracts;
using Common.Observe;
using Microsoft.AspNetCore.Mvc;
using Peloton;

namespace Api.Controllers
{
	[ApiController]
	public class PelotonWorkoutsController : Controller
	{
		private readonly IPelotonService _pelotonService;

		public PelotonWorkoutsController(IPelotonService pelotonService)
		{
			_pelotonService = pelotonService;
		}

		[HttpGet]
		[Route("/api/peloton/workouts")]
		public async Task<RecentWorkoutsGetResponse> GetAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonWorkoutsController)}.{nameof(GetAsync)}");

			var recentWorkouts = await _pelotonService.GetRecentWorkoutsAsync(25);

			return new RecentWorkoutsGetResponse() { Items = recentWorkouts.OrderByDescending(i => i.Created_At).ToList() };

		}
	}
}
