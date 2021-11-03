using Common;
using Common.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WebApp.Models;

namespace WebApp.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IDbClient _db;
		private readonly IAppConfiguration _config;

		public HomeController(ILogger<HomeController> logger, IDbClient db, IAppConfiguration config)
		{
			_logger = logger;
			_db = db;
			_config = config;
		}

		[HttpGet]
		[ApiExplorerSettings(IgnoreApi = true)]
		public IActionResult Index()
		{
			var syncTime = _db.GetSyncTime();
			var model = new HomeViewModel()
			{
				SyncEnabled = _config.App.EnablePolling,
				SyncStatus = syncTime.SyncStatus,
				LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
				LastSyncTime = syncTime.LastSyncTime,
				NextSyncTime = syncTime.NextSyncTime
			};
			return View(model);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
