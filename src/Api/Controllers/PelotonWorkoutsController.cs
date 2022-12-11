using Common.Dto;
using Common.Dto.Api;
using Common.Dto.Peloton;
using Common.Helpers;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc;
using Peloton;
using Peloton.Dto;

namespace Api.Controllers
{
	[ApiController]
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
		[Route("api/peloton/workouts")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<PelotonWorkoutsGetResponse>> GetAsync([FromQuery]PelotonWorkoutsGetRequest request)
		{
			if (!request.IsValid(out var result))
				return result;

			PagedPelotonResponse<Workout>? recentWorkouts = null;

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
				Items = recentWorkouts.data
						.OrderByDescending(i => i.Created_At)
						.Select(w => new PelotonWorkout(w))
						.ToList() 
			};
		}

		/// <summary>
		/// Fetches a list of all workouts recorded on Peloton since a certain date till now, ordered by Most Recent first.
		/// </summary>
		/// <response code="200">Returns the list of recent peloton workouts.</response>
		/// <response code="400">Invalid request values.</response>
		/// <response code="500">Unhandled exception.</response>
		[HttpGet]
		[Route("api/peloton/workouts/all")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<PelotonWorkoutsGetAllResponse>> GetAsync([FromQuery] PelotonWorkoutsGetAllRequest request)
		{
			if (request.SinceDate.IsAfter(DateTime.UtcNow, nameof(request.SinceDate), out var result))
				return result;

			ICollection<Workout> workoutsToReturn = new List<Workout>();
			var completedOnly = request.WorkoutStatusFilter == WorkoutStatus.Completed;

			try
			{
				var serviceResult = await _pelotonService.GetWorkoutsSinceAsync(request.SinceDate);

				if (serviceResult.IsErrored())
					return serviceResult.GetResultForError();

				foreach (var w in serviceResult.Result)
				{
					if (completedOnly && w.Status != "COMPLETE")
						continue;

					if (request.ExcludeWorkoutTypes.Contains(w.GetWorkoutType()))
						continue;

					workoutsToReturn.Add(w);
				}
			}
			catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}

			return new PelotonWorkoutsGetAllResponse()
			{
				SinceDate = request.SinceDate,
				Items = workoutsToReturn
						.OrderByDescending(i => i.Created_At)
						.Select(w => new PelotonWorkout(w))
						.ToList()
			};
		}
	}
}
