using Common.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Controllers
{
    [ApiController]
	public class SettingsController : Controller
	{
		private ISettingsService _settingsService;

		public SettingsController(ISettingsService settingsService)
		{
			_settingsService = settingsService;
		}

		[HttpGet]
		[Route("/settings")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> Index()
		{
			return View(await GetDataAsync());
		}

		[HttpGet]
		[Route("/api/settngs")]
		public Task<SettingsGetResponse> Get()
		{
			return GetDataAsync();
		}

		private async Task<SettingsGetResponse> GetDataAsync()
		{
			var settings = await _settingsService.GetSettingsAsync();
			var appConfig = _settingsService.GetAppConfiguration();

			var response = new SettingsGetResponse() 
			{
				Settings = settings,
				App = appConfig
			};

			response.Settings.Peloton.Email = string.IsNullOrEmpty(response.Settings.Peloton.Email) ? "not set"
												: "******" + response.Settings.Peloton.Email.Substring(6);
			response.Settings.Peloton.Password = string.IsNullOrEmpty(response.Settings.Peloton.Password) ? "not set" : "******";

			response.Settings.Garmin.Email = string.IsNullOrEmpty(response.Settings.Garmin.Email) ? "not set"
												: "******" + response.Settings.Garmin.Email.Substring(6);
			response.Settings.Garmin.Password = string.IsNullOrEmpty(response.Settings.Garmin.Password) ? "not set" : "******";

			return response;
		}

		[HttpPost]
		[Route("/settings")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> SaveAsync([FromForm] SettingsGetResponse request)
        {
			// TODO: Validation
			await _settingsService.UpdateSettings(request.Settings);

			return View("Index");
        }
	}
}
