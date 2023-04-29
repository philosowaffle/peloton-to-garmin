using Api.Contract;
using Common;
using Common.Dto;
using Core.GitHub;
using Philosowaffle.Capability.ReleaseChecks.Model;

namespace Api.Services;

public interface ISystemInfoService
{
	Task<SystemInfoGetResponse> GetAsync(SystemInfoGetRequest request, string? scheme = null, string? host = null);
}

public class SystemInfoService : ISystemInfoService
{
	private readonly IGitHubReleaseCheckService _gitHubService;

	public SystemInfoService(IGitHubReleaseCheckService gitHubService)
	{
		_gitHubService = gitHubService;
	}

	public async Task<SystemInfoGetResponse> GetAsync(SystemInfoGetRequest request, string? scheme = null, string? host = null)
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
			Api = $"{scheme}://{host}/swagger"
		};
	}
}
