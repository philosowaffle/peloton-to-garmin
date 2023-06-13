using Common.Database;
using Common.Dto.Api;
using Common.Helpers;
using Common.Service;
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
	private readonly ISettingsService _settingsService;
	private readonly ISyncService _syncService;
	private readonly ISyncStatusDb _db;

	public SyncController(ISettingsService settingsService, ISyncService syncService, ISyncStatusDb db)
	{
		_settingsService = settingsService;
		_syncService = syncService;
		_db = db;
	}

	/// <summary>
	/// Syncs a given set of workouts from Peloton to Garmin.
	/// </summary>
	/// <response code="201">The sync was successful. Returns the sync status information.</response>
	/// <response code="200">This request completed, but the Sync may not have been successful. Returns the sync status information.</response>
	/// <response code="400">If the request fields are invalid.</response>
	/// <response code="401">ErrorCode.NeedToInitGarminMFAAuth - Garmin Two Factor is enabled, you must manually initialize Garmin auth prior to syncing.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpPost]
	[ProducesResponseType(typeof(SyncPostResponse), StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(SyncPostResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<SyncPostResponse>> SyncAsync([FromBody] SyncPostRequest request)
	{
		var settings = await _settingsService.GetSettingsAsync();

		var (isValid, result) = await IsValidAsync(request);
		if (!isValid) return result;

		SyncResult syncResult = new();
		try
		{
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
		var syncTimeTask = _db.GetSyncStatusAsync();
		var settingsTask = _settingsService.GetSettingsAsync();

		await Task.WhenAll(syncTimeTask, settingsTask);

		var syncTime = await syncTimeTask;
		var settings = await settingsTask;

		var response = new SyncGetResponse()
		{
			SyncEnabled = settings.App.EnablePolling,
			SyncStatus = syncTime.SyncStatus,
			LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
			LastSyncTime = syncTime.LastSyncTime,
			NextSyncTime = syncTime.NextSyncTime
		};

		return response;
	}

	async Task<(bool, ActionResult)> IsValidAsync(SyncPostRequest request)
	{
		ActionResult result = new OkResult();

		if (request.CheckIsNull("PostRequest", out result))
			return (false, result);

		var settings = await _settingsService.GetSettingsAsync();
		if (settings.Garmin.Upload && settings.Garmin.TwoStepVerificationEnabled)
		{
			var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);
			if (auth is null || !auth.IsValid(settings))
			{
				result = new UnauthorizedObjectResult(new ErrorResponse("Must initialize Garmin two factor auth token before sync can be preformed.", ErrorCode.NeedToInitGarminMFAAuth));
				return (false, result);
			}
		}

		if (request.WorkoutIds.CheckDoesNotHaveAny(nameof(request.WorkoutIds), out result))
			return (false, result);

		return (true, result);
	}
}
