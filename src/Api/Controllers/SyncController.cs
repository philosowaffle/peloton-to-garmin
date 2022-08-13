using Api.Contracts;
using Common;
using Common.Database;
using Common.Observe;
using Microsoft.AspNetCore.Mvc;
using Sync;
using static Prometheus.DotNetRuntime.EventListening.Parsers.ExceptionEventParser.Events;
using ILogger = Serilog.ILogger;

namespace Api.Controllers;

[ApiController]
[Route("api/sync")]
[Produces("application/json")]
[Consumes("application/json")]
public class SyncController : Controller
{
	private static readonly ILogger _logger = LogContext.ForClass<SyncController>();

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
	/// <response code="204">Returns the sync status information.</response>
	/// <response code="422">If the request fields are invalid.</response>
	[HttpPost]
	[ProducesResponseType(typeof(SyncPostResponse), 204)]
	public async Task<ActionResult<SyncPostResponse>> SyncAsync([FromBody] SyncPostRequest request)
	{
		using var tracing = Tracing.Trace($"{nameof(SyncController)}.{nameof(SyncAsync)}");

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

		return Ok(response);
	}

	/// <summary>
	/// Fetches the current Sync status.
	/// </summary>
	/// <response code="200">Returns the sync status information.</response>
	[HttpGet]
	[ProducesResponseType(typeof(SyncGetResponse), 200)]
	public async Task<SyncGetResponse> GetAsync()
	{
		using var tracing = Tracing.Trace($"{nameof(SyncController)}.{nameof(GetAsync)}");

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
