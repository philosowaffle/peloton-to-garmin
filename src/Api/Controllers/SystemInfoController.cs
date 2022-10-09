using Common;
using Common.Dto;
using Common.Dto.Api;
using GitHub;
using GitHub.Dto;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("api/systeminfo")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class SystemInfoController : Controller
	{
		private readonly IGitHubService _gitHubService;

		public SystemInfoController(IGitHubService gitHubService) 
		{
			_gitHubService = gitHubService;
		}

		/// <summary>
		/// Fetches information about the service and system.
		/// </summary>
		/// <response code="200">Returns the system information</response>
		[HttpGet]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<SystemInfoGetResponse>> GetAsync([FromQuery]SystemInfoGetRequest request)
		{
			P2GLatestRelease? versionInformation = null;

			if (request.CheckForUpdate)
				versionInformation = await _gitHubService.GetLatestReleaseAsync();

			return new SystemInfoGetResponse()
			{
				OperatingSystem = SystemInformation.OS,
				OperatingSystemVersion = SystemInformation.OSVersion,

				RunTimeVersion = SystemInformation.RunTimeVersion,

				Version = Constants.AppVersion,
				NewerVersionAvailable = versionInformation?.IsReleaseNewerThanInstalledVersion,
				LatestVersionInformation = request.CheckForUpdate ? new LatestVersionInformation()
				{
					LatestVersion = versionInformation?.LatestVersion,
					ReleaseDate = versionInformation?.ReleaseDate.ToString(),
					ReleaseUrl = versionInformation?.ReleaseUrl,
					Description = versionInformation?.Description
				} : null,

				GitHub = "https://github.com/philosowaffle/peloton-to-garmin",
				Documentation = "https://philosowaffle.github.io/peloton-to-garmin/",
				Forums = "https://github.com/philosowaffle/peloton-to-garmin/discussions",
				Donate = "https://www.buymeacoffee.com/philosowaffle",
				Issues = "https://github.com/philosowaffle/peloton-to-garmin/issues",
				Api = $"{this.Request.Scheme}://{this.Request.Host}/swagger"
			};
		}
	}
}
