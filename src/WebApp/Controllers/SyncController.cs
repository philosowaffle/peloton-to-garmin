using Common;
using Common.Database;
using Common.Observe;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Sync;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Controllers
{
    [ApiController]
	public class SyncController : Controller
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncController>();

		private readonly Settings _config;
		private readonly ISyncService _syncService;
		private readonly ISyncStatusDb _db;

		public SyncController(Settings appConfiguration, ISyncService syncService, ISyncStatusDb db)
		{
			_config = appConfiguration;
			_syncService = syncService;
			_db = db;
		}

		[HttpGet]
		[Route("/sync")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> Index()
		{
			using var tracing = Tracing.Trace($"{nameof(SyncController)}.{nameof(Index)}");

			var syncTime = await _db.GetSyncStatusAsync();

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
			using var tracing = Tracing.Trace($"{nameof(SyncController)}.{nameof(Post)}");

			var model = new SyncViewModel();
			var syncResult = await _syncService.SyncAsync(request.NumWorkouts);
			model.Response = new SyncPostResponse() 
			{
				SyncSuccess = syncResult.SyncSuccess,
				PelotonDownloadSuccess = syncResult.PelotonDownloadSuccess,
				ConverToFitSuccess = syncResult.ConversionSuccess,
				UploadToGarminSuccess = syncResult.UploadToGarminSuccess,
				Errors = syncResult.Errors.Select(e => new Models.ErrorResponse(e)).ToList()
			};

			var syncTime = await _db.GetSyncStatusAsync();
			model.GetResponse = new SyncGetResponse()
			{
				SyncEnabled = _config.App.EnablePolling,
				SyncStatus = syncTime.SyncStatus,
				LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
				LastSyncTime = syncTime.LastSyncTime,
				NextSyncTime = syncTime.NextSyncTime,				
			};

			return View("Index", model);
		}

		[HttpPost]
		[Route("/api/sync")]
		[ProducesResponseType(typeof(SyncPostResponse), 200)]
		public async Task<SyncPostResponse> SyncAsync([FromBody] SyncPostRequest request)
		{
			using var tracing = Tracing.Trace($"{nameof(SyncController)}.{nameof(SyncAsync)}");

			if (request.NumWorkouts <= 0)
				throw new Exception(); // TODO: throw correct http error

			var syncResult = await _syncService.SyncAsync(request.NumWorkouts);

			return new SyncPostResponse()
			{
				SyncSuccess = syncResult.SyncSuccess,
				PelotonDownloadSuccess = syncResult.PelotonDownloadSuccess,
				ConverToFitSuccess = syncResult.ConversionSuccess,
				UploadToGarminSuccess = syncResult.UploadToGarminSuccess,
				Errors = syncResult.Errors.Select(e => new Models.ErrorResponse(e)).ToList()
			};
		}
	}
}
