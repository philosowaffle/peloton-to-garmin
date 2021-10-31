using Common;
using Conversion;
using Garmin;
using Microsoft.AspNetCore.Mvc;
using Peloton;
using Serilog;
using System;
using System.Threading.Tasks;
using WebUI.Shared;

namespace WebApp.Controllers
{
	[ApiController]
	public class SyncController : Controller
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncController>();

		private IAppConfiguration _config;
		private IPelotonService _pelotonService;
		private IGarminUploader _garminUploader;
		private IConverter _converter;

		public SyncController(IAppConfiguration appConfiguration, IPelotonService pelotonService, IGarminUploader garminUploader, IConverter converter)
		{
			_config = appConfiguration;
			_pelotonService = pelotonService;
			_garminUploader = garminUploader;
			_converter = converter;
		}

		[HttpGet]
		[Route("/sync")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public ActionResult Index()
		{
			var model = new SyncViewModel();
			return View(model);
		}

		[HttpPost]
		[Route("/sync")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public async Task<ActionResult> Post([FromForm]SyncPostRequest request)
		{
			var model = new SyncViewModel();
			model.Response = await SyncAsync(request.NumWorkouts);
			return View("Index", model);
		}

		[HttpPost]
		[Route("/api/sync")]
		[ProducesResponseType(typeof(SyncPostResponse), 200)]
		public Task<SyncPostResponse> SyncAsync([FromBody] SyncPostRequest request)
		{
			if (request.NumWorkouts <= 0)
				throw new Exception(); // TODO: throw correct http error

			return SyncAsync(request.NumWorkouts);
		}

		private async Task<SyncPostResponse> SyncAsync(int numWorkouts)
		{
			_logger.Information("Reached the SyncController.");
			var response = new SyncPostResponse();

			try
			{
				await _pelotonService.DownloadLatestWorkoutDataAsync(numWorkouts);
				response.PelotonDownloadSuccess = true;

			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to download workouts from Peleoton.");
				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to download workouts from Peloton. Check logs for more details." });
				return response;
			}

			try
			{
				_converter.Convert();
				response.ConverToFitSuccess = true;

			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to convert workouts to FIT format.");
				response.SyncSuccess = false;
				response.ConverToFitSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to convert workouts to FIT format. Check logs for more details." });
				return response;
			}

			try
			{
				await _garminUploader.UploadToGarminAsync();
				response.UploadToGarminSuccess = true;
			}
			catch (GarminUploadException e)
			{
				_logger.Error(e, "GUpload returned an error code. Failed to upload workouts.");
				_logger.Warning("GUpload failed to upload files. You can find the converted files at {@Path} \n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", _config.App.OutputDirectory);

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to upload to Garmin Connect. Check logs for more details." });
				return response;
			}

			response.SyncSuccess = true;
			return response;
		}
	}
}
