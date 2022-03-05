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

			response.GetResponse.Settings.Peloton.Email = string.IsNullOrEmpty(response.GetResponse.Settings.Peloton.Email) ? "not set" : "******";
			response.GetResponse.Settings.Peloton.Password = string.IsNullOrEmpty(response.GetResponse.Settings.Peloton.Password) ? "not set" : "******";

			response.GetResponse.Settings.Garmin.Email = string.IsNullOrEmpty(response.GetResponse.Settings.Garmin.Email) ? "not set" : "******";
			response.GetResponse.Settings.Garmin.Password = string.IsNullOrEmpty(response.GetResponse.Settings.Garmin.Password) ? "not set" : "******";

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

			await _settingsService.UpdateSettings(request.GetResponse.Settings);

			return View("Index", await GetDataAsync());
        }
	}
}
