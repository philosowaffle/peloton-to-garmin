using Common;
using Common.Database;
using Common.Dto.Api;
using Microsoft.AspNetCore.Mvc;
using Sync;
using ErrorResponse = Common.Dto.Api.ErrorResponse;

namespace Api.Controllers;

[ApiController]
[Route("api/sync")]
[Produces("application/json")]
[Consumes("application/json")]
public class SyncController : Controller
{
	private readonly Settings _config;
	private readonly ISyncService _syncService;
	private readonly ISyncStatusDb _db;

	public SyncController(Settings appConfiguration, ISyncService syncService, ISyncStatusDb db)
	{
		_config = appConfiguration;
		_syncService = syncService;
		_db = db;
	}

	/// <summary>
	/// Syncs a given set of workouts from Peloton to Garmin.
	/// </summary>
	/// <response code="201">The sync was successful. Returns the sync status information.</response>
	/// <response code="200">This request completed, but the Sync may not have been successful. Returns the sync status information.</response>
	/// <response code="400">If the request fields are invalid.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpPost]
	[ProducesResponseType(typeof(SyncPostResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(SyncPostResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<SyncPostResponse>> SyncAsync([FromBody] SyncPostRequest request)
	{
		if (request is null ||
			(request.NumWorkouts <= 0 && (request.WorkoutIds is null || !request.WorkoutIds.Any())))
			return BadRequest(new ErrorResponse("Either NumWorkouts or WorkoutIds must be set."));

		if (request.NumWorkouts > 0 && (request.WorkoutIds is not null && request.WorkoutIds.Any()))
			return BadRequest(new ErrorResponse("NumWorkouts and WorkoutIds cannot both be set."));

		SyncResult syncResult = new();
		try
		{
			if (request.NumWorkouts > 0)
				syncResult = await _syncService.SyncAsync(request.NumWorkouts);
			else if (request.WorkoutIds is not null)
				syncResult = await _syncService.SyncAsync(request.WorkoutIds, exclude: null);
		}
		catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}

		var response = new SyncPostResponse()
		{
			SyncSuccess = syncResult.SyncSuccess,
			PelotonDownloadSuccess = syncResult.PelotonDownloadSuccess,
			ConverToFitSuccess = syncResult.ConversionSuccess,
			UploadToGarminSuccess = syncResult.UploadToGarminSuccess,
			Errors = syncResult.Errors.Select(e => new ErrorResponse(e.Message)).ToList()
		};

		if (!response.SyncSuccess)
			return Ok(response);

		return Created("/sync", response);
	}

	/// <summary>
	/// Fetches the current Sync status.
	/// </summary>
	/// <response code="200">Returns the sync status information.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpGet]
	[ProducesResponseType(typeof(SyncGetResponse), 200)]
	public async Task<ActionResult<SyncGetResponse>> GetAsync()
	{
		var syncTime = await _db.GetSyncStatusAsync();

		var response = new SyncGetResponse()
		{
			SyncEnabled = _config.App.EnablePolling,
			SyncStatus = syncTime.SyncStatus,
			LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
			LastSyncTime = syncTime.LastSyncTime,
			NextSyncTime = syncTime.NextSyncTime
		};

		return response;
	}
}
