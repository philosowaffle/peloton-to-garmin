using Common;
using Microsoft.AspNetCore.Mvc;
using WebUI.Shared;

namespace WebApp.Controllers
{
	[ApiController]
	public class SettingsController : Controller
	{
		private IAppConfiguration _config;

		public SettingsController(IAppConfiguration config)
		{
			_config = config;
		}

		[HttpGet]
		[Route("/settings")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public ActionResult Index()
		{
			return View(GetData());
		}

		[HttpGet]
		[Route("/api/settngs")]
		public SettingsGetResponse Get()
		{
			return GetData();
		}

		private SettingsGetResponse GetData()
		{
			var response = new SettingsGetResponse(_config);

			response.Peloton.Email = "******" + _config.Peloton.Email.Substring(6);
			response.Peloton.Password = string.IsNullOrEmpty(_config.Peloton.Password) ? "not set" : "******";

			response.Garmin.Email = "******" + _config.Garmin.Email.Substring(6);
			response.Garmin.Password = string.IsNullOrEmpty(_config.Garmin.Password) ? "not set" : "******";

			return response;
		}
	}
}
