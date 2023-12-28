using Api.Contract;
using Api.Service;
using Api.Service.Helpers;
using Common;
using Common.Dto;
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
	private readonly ISettingsUpdaterService _settingsUpdaterService;

	public SettingsController(ISettingsService settingsService, IFileHandling fileHandler, ISettingsUpdaterService settingsUpdaterService)
	{
		_settingsService = settingsService;
		_fileHandler = fileHandler;
		_settingsUpdaterService = settingsUpdaterService;
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
		try
		{
			var result = await _settingsUpdaterService.UpdateAppSettingsAsync(updatedAppSettings);

			if (result.IsErrored())
				return result.GetResultForError();

			return Ok(result.Result);
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
		try
		{
			var result = await _settingsUpdaterService.UpdateFormatSettingsAsync(updatedFormatSettings);

			if (result.IsErrored())
				return result.GetResultForError();

			return Ok(result.Result);
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
		try
		{
			var result = await _settingsUpdaterService.UpdatePelotonSettingsAsync(updatedPelotonSettings);

			if (result.IsErrored())
				return result.GetResultForError();

			return Ok(result.Result);
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
		try
		{
			var result = await _settingsUpdaterService.UpdateGarminSettingsAsync(updatedGarminSettings);

			if (result.IsErrored())
				return result.GetResultForError();

			return Ok(result.Result);
		} catch (Exception e)
		{
			return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
		}
	}
}