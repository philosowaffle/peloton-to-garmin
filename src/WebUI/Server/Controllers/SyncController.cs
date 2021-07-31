using Common;
using Conversion;
using Garmin;
using Microsoft.AspNetCore.Mvc;
using Peloton;
using Serilog;
using System;
using System.Threading.Tasks;
using WebUI.Shared;

namespace WebUI.Server.Controllers
{
	[ApiController]
	[Route("/api/sync")]
	public class SyncController : ControllerBase
	{
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

		[HttpPost]
		[ProducesResponseType(typeof(SyncPostResponse), 200)]
		[ProducesResponseType(typeof(ErrorResponse), 422)]
		public async Task<IActionResult> PostAsync([FromBody] SyncPostRequest request)
		{
			Log.Information("Reached the SyncController.");
			var response = new SyncPostResponse();

			if (request.NumWorkouts <= 0)
				return UnprocessableEntity(new ErrorResponse() 
				{
					Message = "NumWorkouts must be greater than 0."
				});

			try
			{
				await _pelotonService.DownloadLatestWorkoutDataAsync(request.NumWorkouts);
				response.PelotonDownloadSuccess = true;

			} catch (Exception e)
			{
				Log.Error(e, "Failed to download workouts from Peleoton.");
				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to download workouts from Peloton. Check logs for more details." });
				return Ok(response);
			}

			try
			{
				_converter.Convert();
				response.ConverToFitSuccess = true;

			} catch (Exception e)
			{
				Log.Error(e, "Failed to convert workouts to FIT format.");
				response.SyncSuccess = false;
				response.ConverToFitSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to convert workouts to FIT format. Check logs for more details." });
				return Ok(response);
			}

			try
			{
				await _garminUploader.UploadToGarminAsync();
				response.UploadToGarminSuccess = true;
			}
			catch (GarminUploadException e) 
			{
				Log.Error(e, "GUpload returned an error code. Failed to upload workouts.");
				Log.Warning("GUpload failed to upload files. You can find the converted files at {@Path} \n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", _config.App.OutputDirectory);

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to upload to Garmin Connect. Check logs for more details." });
				return Ok(response);
			}

			response.SyncSuccess = true;
			return Ok(response);
		}
	}
}
