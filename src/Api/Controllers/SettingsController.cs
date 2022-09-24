using Common;
using Common.Dto.Api;
using Common.Observe;
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
	private readonly AppConfiguration _appConfiguration;

	public SettingsController(ISettingsService settingsService, AppConfiguration appConfiguration)
	{
		_settingsService = settingsService;
		_appConfiguration = appConfiguration;
	}

	/// <summary>
	/// Get the current settings.
	/// </summary>
	/// <response code="200">Returns the settings</response>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<SettingsGetResponse> Get()
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Get)}");

		var settings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(settings);
		settingsResponse.Peloton.Password = null;
		settingsResponse.Garmin.Password = null;

		return settingsResponse;
	}

	/// <summary>
	/// Create or update all settings.
	/// </summary>
	/// <response code="200">Returns the settings</response>
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<SettingsGetResponse> Post([FromBody]Settings updatedSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Post)}");

		// TODO: Validation

		await _settingsService.UpdateSettingsAsync(updatedSettings);

		var settings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(settings);
		settingsResponse.Peloton.Password = null;
		settingsResponse.Garmin.Password = null;

		return settingsResponse;
	}

	/// <summary>
	/// Update App settings.
	/// </summary>
	/// <response code="200">Returns the app settings</response>
	[HttpPost]
	[Route("/api/settings/app")]
	public async Task<App> AppPost([FromBody] App updatedAppSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(AppPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.App = updatedAppSettings;

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		return updatedSettings.App;
	}

	/// <summary>
	/// Update Format settings.
	/// </summary>
	/// <response code="200">Returns the format settings</response>
	[HttpPost]
	[Route("/api/settings/format")]
	public async Task<Format> FormatPost([FromBody] Format updatedFormatSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(FormatPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Format = updatedFormatSettings;

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		return updatedSettings.Format;
	}

	/// <summary>
	/// Update Peloton settings.
	/// </summary>
	/// <response code="200">Returns the Peloton settings</response>
	[HttpPost]
	[Route("/api/settings/peloton")]
	public async Task<SettingsPelotonGetResponse> PelotonPost([FromBody] Common.Peloton updatedPelotonSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(PelotonPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Peloton = updatedPelotonSettings;

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(updatedSettings);
		settingsResponse.Peloton.Password = null;

		return settingsResponse.Peloton;
	}

	/// <summary>
	/// Update Garmin settings.
	/// </summary>
	/// <response code="200">Returns the Garmin settings</response>
	[HttpPost]
	[Route("/api/settings/garmin")]
	public async Task<SettingsGarminGetResponse> GarminPost([FromBody] Common.Garmin updatedGarminSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(GarminPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin = updatedGarminSettings;

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(updatedSettings);
		settingsResponse.Garmin.Password = null;

		return settingsResponse.Garmin;
	}
}