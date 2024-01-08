using Api.Contract;
using Common;
using Common.Dto;
using Common.Service;
using Common.Stateful;

namespace Api.Service;

public interface ISettingsUpdaterService
{
	Task<ServiceResult<App>> UpdateAppSettingsAsync(App updatedAppSettings);
	Task<ServiceResult<SettingsPelotonGetResponse>> UpdatePelotonSettingsAsync(SettingsPelotonPostRequest updatedPelotonSettings);
	Task<ServiceResult<Format>> UpdateFormatSettingsAsync(Format updatedFormatSettings);
	Task<ServiceResult<SettingsGarminGetResponse>> UpdateGarminSettingsAsync(SettingsGarminPostRequest updatedGarminSettings);
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
			result.Error = new ServiceError() { Message = "Updated AppSettings must not be null or empty." };
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

	public async Task<ServiceResult<Format>> UpdateFormatSettingsAsync(Format updatedFormatSettings)
	{
		var result = new ServiceResult<Format>();

		if (updatedFormatSettings is null)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Updated Format Settings must not be null or empty." };
			return result;
		}

		if (!string.IsNullOrWhiteSpace(updatedFormatSettings.DeviceInfoPath)
			&& !_fileHandler.FileExists(updatedFormatSettings.DeviceInfoPath))
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "The DeviceInfo path is either not accessible or does not exist." };
			return result;
		}

		var settings = await _settingsService.GetSettingsAsync();
		settings.Format = updatedFormatSettings;

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		result.Result = updatedSettings.Format;
		return result;
	}

	public async Task<ServiceResult<SettingsGarminGetResponse>> UpdateGarminSettingsAsync(SettingsGarminPostRequest updatedGarminSettings)
	{
		var result = new ServiceResult<SettingsGarminGetResponse>();

		if (updatedGarminSettings is null)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Updated Garmin Settings must not be null or empty." };
			return result;
		}

		var settings = await _settingsService.GetSettingsAsync();
		settings.Garmin = updatedGarminSettings.Map();

		if (settings.Garmin.Upload && settings.Garmin.TwoStepVerificationEnabled && settings.App.EnablePolling)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Garmin TwoStepVerification cannot be enabled while Automatic Syncing is enabled. Please disable Automatic Syncing first." };
			return result;
		}

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		result.Result = new SettingsGetResponse(updatedSettings).Garmin;
		return result;
	}

	public async Task<ServiceResult<SettingsPelotonGetResponse>> UpdatePelotonSettingsAsync(SettingsPelotonPostRequest updatedPelotonSettings)
	{
		var result = new ServiceResult<SettingsPelotonGetResponse>();

		if (updatedPelotonSettings is null)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Updated PelotonSettings must not be null or empty." };
			return result;
		}

		var settings = await _settingsService.GetSettingsAsync();

		if (updatedPelotonSettings.NumWorkoutsToDownload <= 0
			&& settings.App.EnablePolling)
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "Number of workouts to download must but greater than 0 when Automatic Polling is enabled." };
			return result;
		}

		settings.Peloton = updatedPelotonSettings.Map();

		await _settingsService.UpdateSettingsAsync(settings);
		var updatedSettings = await _settingsService.GetSettingsAsync();

		result.Result = new SettingsGetResponse(updatedSettings).Peloton;
		return result;
	}
}
