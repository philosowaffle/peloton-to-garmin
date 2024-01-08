using Api.Contract;
using Api.Service;
using Common;
using Common.Dto;
using Common.Observe;
using Common.Service;
using Philosowaffle.Capability.ReleaseChecks.Model;

namespace Api.Services;

public interface ISystemInfoService
{
	Task<SystemInfoGetResponse> GetAsync(SystemInfoGetRequest request, string? scheme = null, string? host = null);
	Task<ServiceResult<SystemInfoLogsGetResponse>> GetLogsAsync();
}

public class SystemInfoService : ISystemInfoService
{
	private readonly IVersionInformationService _versionInformationService;
	private readonly ISettingsService _settingsService;

	public SystemInfoService(ISettingsService settingsService, IVersionInformationService versionInformationService)
	{
		_versionInformationService = versionInformationService;
		_settingsService = settingsService;
	}

	public async Task<SystemInfoGetResponse> GetAsync(SystemInfoGetRequest request, string? scheme = null, string? host = null)
	{
		LatestReleaseInformation? versionInformation = null;

		if (request.CheckForUpdate)
			versionInformation = await _versionInformationService.GetLatestReleaseInformationAsync();

		var documentationVersion = versionInformation?.IsInstalledVersionReleaseCandidate ?? true ? "master" : $"v{Constants.AppVersion}";

		var settings = await _settingsService.GetSettingsAsync();

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
			Documentation = $"https://philosowaffle.github.io/peloton-to-garmin/{documentationVersion}",
			Forums = "https://github.com/philosowaffle/peloton-to-garmin/discussions",
			Donate = "https://www.buymeacoffee.com/philosowaffle",
			Issues = "https://github.com/philosowaffle/peloton-to-garmin/issues",
			Api = $"{scheme}://{host}/swagger",

			OutputDirectory = settings?.App?.OutputDirectory ?? string.Empty,
			TempDirectory = settings?.App?.WorkingDirectory ?? string.Empty,
		};
	}

	public async Task<ServiceResult<SystemInfoLogsGetResponse>> GetLogsAsync()
	{
		var result = new ServiceResult<SystemInfoLogsGetResponse>();

		if (string.IsNullOrWhiteSpace(Logging.CurrentFilePath))
		{
			result.Error = new ServiceError() { Message = "No log file path found." };
			return result;
		}

		var text = string.Empty;
		try
		{
			using (var sr = new StreamReader(Logging.CurrentFilePath, new FileStreamOptions () { Access = FileAccess.Read, Share = FileShare.ReadWrite }))
			{
				text = await sr.ReadToEndAsync();

				result.Result = new SystemInfoLogsGetResponse() { LogText = text };
				result.Successful = true;
				return result;
			}
		}
		catch (FileNotFoundException ex)
		{
			result.Error = new ServiceError() { Message = "Failed to read logs", Exception = ex, IsServerException = true };
		}

		return result;
	}
}
