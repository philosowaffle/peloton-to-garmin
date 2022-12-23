using Common;
using Common.Dto.Api;
using Common.Helpers;
using Common.Service;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/settings")]
[Produces("application/json")]
[Consumes("application/json")]
public class SettingsController : Controller
{
	private readonly ISettingsService _settingsService;
	private readonly IFileHandling _fileHandler;

	public SettingsController(ISettingsService settingsService, IFileHandling fileHandler)
	{
		_settingsService = settingsService;
		_fileHandler = fileHandler;
	}

	/// <summary>
	/// Get the current settings.
	/// </summary>
	/// <response code="200">Returns the settings</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<SettingsGetResponse>> Get()
	{
		try
		{
			var settings = await _settingsService.GetSettingsAsync();

			var settingsResponse = new SettingsGetResponse(settings);
			settingsResponse.Peloton.Password = null;
			settingsResponse.Garmin.Password = null;

			return settingsResponse;
		} catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}

	/// <summary>
	/// Update App settings.
	/// </summary>
	/// <response code="200">Returns the app settings</response>
	/// <response code="400">If the request fields are invalid.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpPost]
	[Route("/api/settings/app")]
	[ProducesResponseType(typeof(App), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<App>> AppPost([FromBody] App updatedAppSettings)
	{
		if (updatedAppSettings.CheckIsNull("PostRequest", out var result))
			return result;

		if (!string.IsNullOrWhiteSpace(updatedAppSettings.OutputDirectory)
			&& !_fileHandler.DirExists(updatedAppSettings.OutputDirectory))
			return new BadRequestObjectResult(new ErrorResponse($"Output Directory path is either not accessible or does not exist."));

		if (updatedAppSettings.EnablePolling 
			&& updatedAppSettings.PollingIntervalSeconds.CheckIsLessThanOrEqualTo(0, "PollingIntervalSeconds", out result))
			return result;

		try
		{
			var settings = await _settingsService.GetSettingsAsync();
			settings.App = updatedAppSettings;

			await _settingsService.UpdateSettingsAsync(settings);
			var updatedSettings = await _settingsService.GetSettingsAsync();

			return Ok(updatedSettings.App);
		} catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}

	/// <summary>
	/// Update Format settings.
	/// </summary>
	/// <response code="200">Returns the format settings</response>
	/// <response code="400">If the request fields are invalid.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpPost]
	[Route("/api/settings/format")]
	[ProducesResponseType(typeof(App), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<Format>> FormatPost([FromBody] Format updatedFormatSettings)
	{
		if (updatedFormatSettings.CheckIsNull("PostRequest", out var result))
			return result;

		if (!string.IsNullOrWhiteSpace(updatedFormatSettings.DeviceInfoPath)
			&& !_fileHandler.DirExists(updatedFormatSettings.DeviceInfoPath))
			return new BadRequestObjectResult(new ErrorResponse($"DeviceInfo path is either not accessible or does not exist."));

		try
		{
			var settings = await _settingsService.GetSettingsAsync();
			settings.Format = updatedFormatSettings;

			await _settingsService.UpdateSettingsAsync(settings);
			var updatedSettings = await _settingsService.GetSettingsAsync();

			return Ok(updatedSettings.Format);
		} catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}

	/// <summary>
	/// Update Peloton settings.
	/// </summary>
	/// <response code="200">Returns the format settings</response>
	/// <response code="400">If the request fields are invalid.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpPost]
	[Route("/api/settings/peloton")]
	[ProducesResponseType(typeof(App), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<SettingsPelotonGetResponse>> PelotonPost([FromBody] SettingsPelotonPostRequest updatedPelotonSettings)
	{
		if (updatedPelotonSettings.CheckIsNull("PostRequest", out var result))
			return result;

		if (updatedPelotonSettings.NumWorkoutsToDownload.CheckIsLessThanOrEqualTo(0, "NumWorkoutsToDownload", out result))
			return result;

		try
		{
			var settings = await _settingsService.GetSettingsAsync();
			settings.Peloton = updatedPelotonSettings.Map();

			await _settingsService.UpdateSettingsAsync(settings);
			var updatedSettings = await _settingsService.GetSettingsAsync();

			var settingsResponse = new SettingsGetResponse(updatedSettings);

			return Ok(settingsResponse.Peloton);
		} catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}

	/// <summary>
	/// Update Garmin settings.
	/// </summary>
	/// <response code="200">Returns the Garmin settings</response>
	/// <response code="400">If the request fields are invalid.</response>
	/// <response code="500">Unhandled exception.</response>
	[HttpPost]
	[Route("/api/settings/garmin")]
	[ProducesResponseType(typeof(App), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
	public async Task<ActionResult<SettingsGarminGetResponse>> GarminPost([FromBody] SettingsGarminPostRequest updatedGarminSettings)
	{
		if (updatedGarminSettings.CheckIsNull("PostRequest", out var result))
			return result;

		try
		{
			var settings = await _settingsService.GetSettingsAsync();
			settings.Garmin = updatedGarminSettings.Map();

			await _settingsService.UpdateSettingsAsync(settings);
			var updatedSettings = await _settingsService.GetSettingsAsync();

			var settingsResponse = new SettingsGetResponse(updatedSettings);

			return Ok(settingsResponse.Garmin);
		} catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}
}