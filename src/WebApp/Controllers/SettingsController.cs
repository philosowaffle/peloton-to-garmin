using Common;
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
			return View(await GetDataAsync());
		}

		[HttpGet]
		[Route("/api/settngs")]
		public async Task<SettingsGetResponse> Get()
		{
			return (await GetDataAsync()).GetResponse;
		}

		private async Task<SettingsViewModel> GetDataAsync()
		{
			var settings = await _settingsService.GetSettingsAsync();

			var response = new SettingsViewModel() 
			{
				GetResponse = new SettingsGetResponse()
				{
					Settings = settings,
					App = _appConfiguration
				}
			};

			response.GetResponse.Settings.Peloton.Email = string.IsNullOrEmpty(response.GetResponse.Settings.Peloton.Email) ? "not set"
												: "******" + response.GetResponse.Settings.Peloton.Email.Substring(6);
			response.GetResponse.Settings.Peloton.Password = string.IsNullOrEmpty(response.GetResponse.Settings.Peloton.Password) ? "not set" : "******";

			response.GetResponse.Settings.Garmin.Email = string.IsNullOrEmpty(response.GetResponse.Settings.Garmin.Email) ? "not set"
												: "******" + response.GetResponse.Settings.Garmin.Email.Substring(6);
			response.GetResponse.Settings.Garmin.Password = string.IsNullOrEmpty(response.GetResponse.Settings.Garmin.Password) ? "not set" : "******";

			return response;
		}

		[HttpPost]
		[Route("/settings")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> SaveAsync([FromForm] SettingsViewModel request)
        {
			// TODO: Validation
			await _settingsService.UpdateSettings(request.GetResponse.Settings);

			return View("Index", await GetDataAsync());
        }
	}
}
