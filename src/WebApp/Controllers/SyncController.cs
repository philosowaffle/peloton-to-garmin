using Common;
using Common.Database;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading.Tasks;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
	[ApiController]
	public class SyncController : Controller
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncController>();

		private readonly IAppConfiguration _config;
		private readonly ISyncService _syncService;
		private readonly IDbClient _db;

		public SyncController(IAppConfiguration appConfiguration, ISyncService syncService, IDbClient db)
		{
			_config = appConfiguration;
			_syncService = syncService;
			_db = db;
		}

		[HttpGet]
		[Route("/sync")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public ActionResult Index()
		{
			var syncTime = _db.GetSyncStatus();

			var model = new SyncViewModel()
			{
				GetResponse = new SyncGetResponse()
				{
					SyncEnabled = _config.App.EnablePolling,
					SyncStatus = syncTime.SyncStatus,
					LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
					LastSyncTime = syncTime.LastSyncTime,
					NextSyncTime = syncTime.NextSyncTime
				}
			};
			return View(model);
		}

		[HttpPost]
		[Route("/sync")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> Post([FromForm]SyncPostRequest request)
		{
			var model = new SyncViewModel();
			model.Response = await _syncService.SyncAsync(request.NumWorkouts);

			var syncTime = _db.GetSyncStatus();
			model.GetResponse = new SyncGetResponse()
			{
				SyncEnabled = _config.App.EnablePolling,
				SyncStatus = syncTime.SyncStatus,
				LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
				LastSyncTime = syncTime.LastSyncTime,
				NextSyncTime = syncTime.NextSyncTime
			};

			return View("Index", model);
		}

		[HttpPost]
		[Route("/api/sync")]
		[ProducesResponseType(typeof(SyncPostResponse), 200)]
		public Task<SyncPostResponse> SyncAsync([FromBody] SyncPostRequest request)
		{
			if (request.NumWorkouts <= 0)
				throw new Exception(); // TODO: throw correct http error

			return _syncService.SyncAsync(request.NumWorkouts);
		}
	}
}
