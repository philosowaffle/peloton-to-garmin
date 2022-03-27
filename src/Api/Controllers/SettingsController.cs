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
	public async Task<Settings> Get()
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Get)}");

		var settings = await _settingsService.GetSettingsAsync();

		settings.Peloton.Password = null;
		settings.Garmin.Password = null;

		return settings;
	}

	[HttpPost]
	[Route("/api/settings")]
	public async Task<Settings> Post([FromBody]Settings updatedSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Post)}");

		// TODO: Validation

		await _settingsService.UpdateSettings(updatedSettings);

		return await _settingsService.GetSettingsAsync();
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
	public async Task<Common.Peloton> PelotonPost([FromBody] Common.Peloton updatedPelotonSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(PelotonPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Peloton = updatedPelotonSettings;

		await _settingsService.UpdateSettings(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		return updatedSettings.Peloton;
	}

	[HttpPost]
	[Route("/api/settings/garmin")]
	public async Task<Common.Garmin> GarminPost([FromBody] Common.Garmin updatedGarminSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(GarminPost)}");

		// TODO: Validation

		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin = updatedGarminSettings;

		await _settingsService.UpdateSettings(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		return updatedSettings.Garmin;
	}
}