using Common;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WebUI.Shared;

namespace WebUI.Server.Controllers
{
	[ApiController]
	[Route("/sync")]
	public class SyncController : ControllerBase
	{
		private IAppConfiguration _config;

		public SyncController(IAppConfiguration config)
		{
			_config = config;
		}

		[HttpPost]
		public IActionResult PostAsync([FromBody] SyncPostRequest request)
		{
			Log.Information("Reached the SyncController.");

			if (request.NumWorkouts <= 0)
				return UnprocessableEntity(new ErrorResponse() 
				{
					Message = "NumWorkouts must be greater than 0."
				});

			if (request.NumWorkouts % 2 == 0)
				return Ok(true);

			return Ok(_config.Garmin.Upload);
		}
	}
}
