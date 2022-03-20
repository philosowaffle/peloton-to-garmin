using Common;
using Common.Observe;
using Common.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Controllers
{
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
		[Route("/settings")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> Index()
		{
			using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Index)}");

			return View(await GetDataAsync());
		}

		[HttpGet]
		[Route("/api/settings")]
		public async Task<SettingsGetResponse> Get()
		{
			using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Get)}");

			return (await GetDataAsync()).GetResponse;
		}

		[HttpPost]
		[Route("/api/settings")]
		public async Task<SettingsGetResponse> Post([FromBody]SettingsGetResponse request)
		{
			using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(Post)}");

			var updatedSettings = request.Settings;

			// TODO: Validation

			await _settingsService.UpdateSettings(updatedSettings);

			return (await GetDataAsync()).GetResponse;
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

		private async Task<SettingsViewModel> GetDataAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(GetDataAsync)}");

			var settings = await _settingsService.GetSettingsAsync();

			var response = new SettingsViewModel() 
			{
				GetResponse = new SettingsGetResponse()
				{
					Settings = settings,
					App = _appConfiguration
				}
			};

			return response;
		}

		[HttpPost]
		[Route("/settings")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> SaveAsync([FromForm] SettingsViewModel request)
        {
			using var tracing = Tracing.Trace($"{nameof(SettingsController)}.{nameof(SaveAsync)}");

			var updatedSettings = request.GetResponse.Settings;

			// TODO: Validation

			if (updatedSettings.Garmin.Email == "not set" || updatedSettings.Garmin.Email == "******")
				updatedSettings.Garmin.Email = null;

			if (updatedSettings.Garmin.Password == "not set" || updatedSettings.Garmin.Password == "******")
				updatedSettings.Garmin.Password = null;

			if (updatedSettings.Peloton.Email == "not set" || updatedSettings.Peloton.Email == "******")
				updatedSettings.Peloton.Email = null;

			if (updatedSettings.Peloton.Password == "not set" || updatedSettings.Peloton.Password == "******")
				updatedSettings.Peloton.Password = null;

			await _settingsService.UpdateSettings(updatedSettings);

			return View("Index", await GetDataAsync());
        }
	}
}
