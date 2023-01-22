using Api.Contract;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("api/systeminfo")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class SystemInfoController : Controller
	{
		private readonly ISystemInfoService _systemInfoService;

		public SystemInfoController(ISystemInfoService systemInforService) 
		{
			_systemInfoService = systemInforService;
		}

		/// <summary>
		/// Fetches information about the service and system.
		/// </summary>
		/// <response code="200">Returns the system information</response>
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<SystemInfoGetResponse>> GetAsync([FromQuery]SystemInfoGetRequest request)
		{
			var result = await _systemInfoService.GetAsync(request, this.Request.Scheme, this.Request.Host.ToString());
			return Ok(result);
		}
	}
}
