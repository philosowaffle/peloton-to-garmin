using Api.Contracts;
using Common;
using Common.Observe;
using Common.Service;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class SettingsController : Controller
{
	private readonly ISettingsService _settingsService;
	private readonly AppConfiguration _appConfiguration;

	public SettingsController(ISettingsService settingsService, AppConfiguration appConfiguration)
	{
		_settingsService = settingsService;
		_appConfiguration = appConfiguration;
	}

	[HttpGet]
	[Route("/api/settings")]
	public async Task<SettingsGetResponse> Get()
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Get)}");

		var settings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(settings);
		settingsResponse.Peloton.Password = null;
		settingsResponse.Garmin.Password = null;

		return settingsResponse;
	}

	[HttpPost]
	[Route("/api/settings")]
	public async Task<SettingsGetResponse> Post([FromBody]Settings updatedSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Post)}");

		// TODO: Validation

		await _settingsService.UpdateSettings(updatedSettings);

		var settings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(settings);
		settingsResponse.Peloton.Password = null;
		settingsResponse.Garmin.Password = null;

		return settingsResponse;
	}

	[HttpPost]
	[Route("/api/settings/app")]
	public async Task<App> AppPost([FromBody] App updatedAppSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(AppPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.App = updatedAppSettings;

		await _settingsService.UpdateSettings(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		return updatedSettings.App;
	}

	[HttpPost]
	[Route("/api/settings/format")]
	public async Task<Format> FormatPost([FromBody] Format updatedFormatSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(FormatPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Format = updatedFormatSettings;

		await _settingsService.UpdateSettings(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		return updatedSettings.Format;
	}

	[HttpPost]
	[Route("/api/settings/peloton")]
	public async Task<SettingsPelotonGetResponse> PelotonPost([FromBody] Common.Peloton updatedPelotonSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(PelotonPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Peloton = updatedPelotonSettings;

		await _settingsService.UpdateSettings(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(updatedSettings);
		settingsResponse.Peloton.Password = null;

		return settingsResponse.Peloton;
	}

	[HttpPost]
	[Route("/api/settings/garmin")]
	public async Task<SettingsGarminGetResponse> GarminPost([FromBody] Common.Garmin updatedGarminSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(GarminPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin = updatedGarminSettings;

		await _settingsService.UpdateSettings(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		var settingsResponse = new SettingsGetResponse(updatedSettings);
		settingsResponse.Garmin.Password = null;

		return settingsResponse.Garmin;
	}
}