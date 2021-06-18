using Common;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Server.Controllers
{
	[ApiController]
	[Route("/api/settings")]
	public class SettingsController : Controller
	{
		private IAppConfiguration _config;

		public SettingsController(IAppConfiguration config)
		{
			_config = config;
		}

		[HttpGet]
		public IActionResult Index()
		{
			_config.Peloton.Email = "******" + _config.Peloton.Email.Substring(6);
			_config.Peloton.Password = "******";

			_config.Garmin.Email = "******" + _config.Peloton.Email.Substring(6);
			_config.Garmin.Password = "******";

			return Ok(_config);
		}
	}
}
