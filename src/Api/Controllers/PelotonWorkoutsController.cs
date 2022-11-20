using Common;
using Common.Dto;
using Common.Dto.Api;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Service;
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
		private static readonly Task<Settings> DiscardSettingsTask = Task.FromResult(new Settings());

		private readonly IPelotonService _pelotonService;
		private readonly ISettingsService _settingsService;

		public PelotonWorkoutsController(IPelotonService pelotonService, ISettingsService settingsService)
		{
			_pelotonService = pelotonService;
			_settingsService = settingsService;
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
			if (request.PageSize <= 0)
				return BadRequest(new ErrorResponse("PageSize must be greater than 0."));
			if (request.PageIndex < 0)
				return BadRequest(new ErrorResponse("PageIndex must be greater than or equal to 0."));

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
				Items = recentWorkouts.data.OrderByDescending(i => i.Created_At).ToList() 
			};
		}

		/// <summary>
		/// Fetches a list of workouts recorded on Peloton since a certain date till now, ordered by Most Recent first.
		/// </summary>
		/// <response code="200">Returns the list of recent peloton workouts.</response>
		/// <response code="400">Invalid request values.</response>
		/// <response code="500">Unhandled exception.</response>
		[HttpGet]
		[Route("api/peloton/workouts/sinceDate")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<PelotonWorkoutsSinceGetResponse>> GetAsync([FromQuery] PelotoWorkoutsSinceGetRequest request)
		{
			if (request.SinceDate.IsAfter(DateTime.UtcNow, nameof(request.SinceDate), out var result))
				return result;

			ICollection<Workout> workoutsToReturn = new List<Workout>();

			try
			{
				var getWorkoutsTask = _pelotonService.GetWorkoutsSinceAsync(request.SinceDate);
				Task<Settings> getSettingsTask = DiscardSettingsTask;
				if (request.FilterOutExcludedWorkoutTypes)
					getSettingsTask = _settingsService.GetSettingsAsync();

				var workouts = await getWorkoutsTask;
				Settings? settings = null;
				foreach(var w in workouts)
				{
					if (request.CompletedOnly && w.Status != "COMPLETE")
						continue;

					if (request.FilterOutExcludedWorkoutTypes)
					{
						settings = settings ?? await getSettingsTask;
						if (settings.Peloton.ExcludeWorkoutTypes.Contains(w.GetWorkoutType()))
							continue;
					}

					workoutsToReturn.Add(w);
				}
			}
			catch (ArgumentException ae)
			{
				return BadRequest(new ErrorResponse(ae.Message));
			}
			catch (PelotonAuthenticationError pe)
			{
				return BadRequest(new ErrorResponse(pe.Message));
			}
			catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}

			return new PelotonWorkoutsSinceGetResponse()
			{
				SinceDate = request.SinceDate,
				Items = workoutsToReturn.OrderByDescending(i => i.Created_At).ToList()
			};
		}
	}
}
