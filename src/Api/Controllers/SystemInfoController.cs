using Common;
using Common.Dto;
using Common.Dto.Api;
using Core.GitHub;
using Microsoft.AspNetCore.Mvc;
using Philosowaffle.Capability.ReleaseChecks.Model;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("api/systeminfo")]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class SystemInfoController : Controller
	{
		private readonly IGitHubReleaseCheckService _gitHubService;

		public SystemInfoController(IGitHubReleaseCheckService gitHubService) 
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
			LatestReleaseInformation? versionInformation = null;

			if (request.CheckForUpdate)
				versionInformation = await _gitHubService.GetLatestReleaseInformationAsync("philosowaffle", "peloton-to-garmin", Constants.AppVersion);

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
