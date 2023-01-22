using Common;
using Common.Dto;
using Common.Dto.Api;
using Common.Service;
using Core.GitHub;
using Microsoft.AspNetCore.Http;
using Philosowaffle.Capability.ReleaseChecks.Model;
using SharedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientUI;

public class ServiceClient : IApiClient
{
	private readonly IGitHubReleaseCheckService _gitHubService;
	private readonly ISettingsService _settingsService;

	public ServiceClient(IGitHubReleaseCheckService gitHubService, ISettingsService settingsService)
	{
		_gitHubService = gitHubService;
		_settingsService = settingsService;
	}

	public Task<ProgressGetResponse> GetAnnualProgressAsync()
	{
		throw new NotImplementedException();
	}

	public Task<PelotonWorkoutsGetResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetRequest request)
	{
		throw new NotImplementedException();
	}

	public Task<PelotonWorkoutsGetAllResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetAllRequest request)
	{
		throw new NotImplementedException();
	}

	public Task<Common.App> SettingsAppPostAsync(Common.App appSettings)
	{
		throw new NotImplementedException();
	}

	public Task<Format> SettingsFormatPostAsync(Format formatSettings)
	{
		throw new NotImplementedException();
	}

	public Task<SettingsGarminGetResponse> SettingsGarminPostAsync(SettingsGarminPostRequest garminSettings)
	{
		throw new NotImplementedException();
	}

	public async Task<SettingsGetResponse> SettingsGetAsync()
	{
		try
		{
			var settings = await _settingsService.GetSettingsAsync();

			var settingsResponse = new SettingsGetResponse(settings);
			settingsResponse.Peloton.Password = null;
			settingsResponse.Garmin.Password = null;

			return settingsResponse;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error ocurred: {e.Message}", e);
		}
	}

	public Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(SettingsPelotonPostRequest pelotonSettings)
	{
		throw new NotImplementedException();
	}

	public Task<SyncGetResponse> SyncGetAsync()
	{
		throw new NotImplementedException();
	}

	public Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest)
	{
		throw new NotImplementedException();
	}

	public async Task<SystemInfoGetResponse> SystemInfoGetAsync(SystemInfoGetRequest systemInfoGetRequest)
	{
		LatestReleaseInformation? versionInformation = null;

		if (systemInfoGetRequest.CheckForUpdate)
			versionInformation = await _gitHubService.GetLatestReleaseInformationAsync("philosowaffle", "peloton-to-garmin", Constants.AppVersion);

		return new SystemInfoGetResponse()
		{
			OperatingSystem = SystemInformation.OS,
			OperatingSystemVersion = SystemInformation.OSVersion,

			RunTimeVersion = SystemInformation.RunTimeVersion,

			Version = Constants.AppVersion,
			NewerVersionAvailable = versionInformation?.IsReleaseNewerThanInstalledVersion,
			LatestVersionInformation = systemInfoGetRequest.CheckForUpdate ? new LatestVersionInformation()
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
			Issues = "https://github.com/philosowaffle/peloton-to-garmin/issues"
		};
	}
}
