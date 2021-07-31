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
		[ProducesResponseType(typeof(IAppConfiguration), 200)]
		public IActionResult Index()
		{
			_config.Peloton.Email = "******" + _config.Peloton.Email.Substring(6);
			_config.Peloton.Password = string.IsNullOrEmpty(_config.Peloton.Password) ? "not set" : "******";

			_config.Garmin.Email = "******" + _config.Garmin.Email.Substring(6);
			_config.Garmin.Password = string.IsNullOrEmpty(_config.Garmin.Password) ? "not set" : "******";

			return Ok(_config);
		}
	}
}
