using Common;
using Common.Dto;
using Common.Service;

namespace Api.Service;

public interface ISettingsUpdaterService
{
	Task<ServiceResult<App>> UpdateAppSettingsAsync(App updatedAppSettings);
}
public class SettingsUpdaterService : ISettingsUpdaterService
{
	private readonly IFileHandling _fileHandler;
	private readonly ISettingsService _settingsService;

	public SettingsUpdaterService(IFileHandling fileHandler, ISettingsService settingsService)
	{
		_fileHandler = fileHandler;
		_settingsService = settingsService;
	}

	public async Task<ServiceResult<App>> UpdateAppSettingsAsync(App updatedAppSettings)
	{
		var result = new ServiceResult<App>();

		if (updatedAppSettings is null)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Update AppSettings must not be null or empty." };
			return result;
		}

		if (!string.IsNullOrWhiteSpace(updatedAppSettings.OutputDirectory)
			&& !_fileHandler.DirExists(updatedAppSettings.OutputDirectory))
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Output Directory path is either not accessible or does not exist." };
			return result;
		}

		var settings = await _settingsService.GetSettingsAsync();
		settings.App = updatedAppSettings;

		if (settings.Garmin.TwoStepVerificationEnabled && settings.App.EnablePolling)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Automatic Syncing cannot be enabled when Garmin TwoStepVerification is enabled." };
			return result;
		}

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		result.Result = updatedSettings.App;
		return result;
	}
}
