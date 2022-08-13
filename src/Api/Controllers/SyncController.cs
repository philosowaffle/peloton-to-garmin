using Api.Contracts;
using Common;
using Common.Database;
using Microsoft.AspNetCore.Mvc;
using Sync;

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
	/// <response code="204">The sync was successful. Returns the sync status information.</response>
	/// <response code="200">This request completed, but the Sync may not have been successful. Returns the sync status information.</response>
	/// <response code="400">If the request fields are invalid.</response>
	[HttpPost]
	[ProducesResponseType(typeof(SyncPostResponse), 204)]
	[ProducesResponseType(typeof(SyncPostResponse), 200)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<SyncPostResponse>> SyncAsync([FromBody] SyncPostRequest request)
	{
		if (request is null ||
			(request.NumWorkouts <= 0 && (!request.WorkoutIds?.Any() ?? true)))
			return BadRequest("Either NumWorkouts or WorkoutIds must be set");

		SyncResult syncResult = new();
		if (request.NumWorkouts > 0)
			syncResult = await _syncService.SyncAsync(request.NumWorkouts);
		else
			syncResult = await _syncService.SyncAsync(request.WorkoutIds, exclude: null);

		var response = new SyncPostResponse()
		{
			SyncSuccess = syncResult.SyncSuccess,
			PelotonDownloadSuccess = syncResult.PelotonDownloadSuccess,
			ConverToFitSuccess = syncResult.ConversionSuccess,
			UploadToGarminSuccess = syncResult.UploadToGarminSuccess,
			Errors = syncResult.Errors.Select(e => new Contracts.ErrorResponse(e)).ToList()
		};

		if (!response.SyncSuccess)
			return Ok(response);

		return Created("/sync", response);
	}

	/// <summary>
	/// Fetches the current Sync status.
	/// </summary>
	/// <response code="200">Returns the sync status information.</response>
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

		return Ok(response);
	}
}
