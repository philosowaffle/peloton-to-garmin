﻿using Common;
using Common.Database;
using Common.Observe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly ISyncStatusDb _syncStatusDb;
		private readonly IDbClient _syncHistoryDb;
		private readonly Settings _config;

		public HomeController(ILogger<HomeController> logger, ISyncStatusDb db, Settings config, IDbClient dbClient)
		{
			_logger = logger;
			_syncStatusDb = db;
			_config = config;
			_syncHistoryDb = dbClient;
		}

		[HttpGet]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<IActionResult> Index()
		{
			using var tracing = Tracing.Trace($"{nameof(HomeController)}.{nameof(Index)}");

			var syncTime = await _syncStatusDb.GetSyncStatusAsync();
			var model = new HomeViewModel()
			{
				SyncEnabled = _config.App.EnablePolling,
				SyncStatus = syncTime.SyncStatus,
				LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
				LastSyncTime = syncTime.LastSyncTime,
				NextSyncTime = syncTime.NextSyncTime,
				RecentWorkouts = _syncHistoryDb.GetRecentlySyncedItems(10)
			};
			return View(model);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			using var tracing = Tracing.Trace($"{nameof(HomeController)}.{nameof(Error)}");

			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
