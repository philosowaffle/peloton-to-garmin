using Microsoft.AspNetCore.Mvc;
using WebUI.Shared;

namespace WebUI.Server.Controllers
{
	[ApiController]
	[Route("/sync")]
	public class SyncController : ControllerBase
	{
		[HttpPost]
		public IActionResult PostAsync([FromBody] SyncPostRequest request)
		{
			if (request.NumWorkouts <= 0)
				return UnprocessableEntity(new ErrorResponse() 
				{
					Message = "NumWorkouts must be greater than 0."
				});

			if (request.NumWorkouts % 2 == 0)
				return Ok(true);

			return Ok(false);
		}
	}
}
