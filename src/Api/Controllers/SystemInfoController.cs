using Api.Contract;
using Api.Services;
using Api.Service.Helpers;
using Microsoft.AspNetCore.Mvc;
using Common.Observe;

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

		[HttpGet]
		[Route("/api/systeminfo/logs")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult> LogsGetAsync()
		{
			try
			{
				var result = await _systemInfoService.GetLogsAsync();

				if (result.IsErrored())
					return result.GetResultForError();

				return Ok(result.Result);

			} catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}
		}

		[HttpPost]
		[Route("logLevel")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<LogLevelPostResponse>> LogLevelPostAsync(LogLevelPostRequest request)
		{
			try
			{
				var result = await _systemInfoService.SetLogLevelAsync(request);

				if (result.IsErrored())
					return result.GetResultForError();

				return Created("/systeminfo", new LogLevelPostResponse() { LogLevel = result.Result });

			}
			catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}
		}
	}
}