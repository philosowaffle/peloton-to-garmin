using Api.Contract;
using Common;
using Common.Dto;
using Common.Service;
using Garmin.Auth;

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
	private readonly IGarminAuthenticationService _garminAuthService;

	public SettingsUpdaterService(IFileHandling fileHandler, ISettingsService settingsService, IGarminAuthenticationService garminAuthService)
	{
		_fileHandler = fileHandler;
		_settingsService = settingsService;
		_garminAuthService = garminAuthService;
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

		if (!string.IsNullOrWhiteSpace(updatedGarminSettings.Password)
			&& updatedGarminSettings.Password.Contains('\\'))
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "P2G does not support the `\\` character in passwords." };
			return result;
		}

		var settings = await _settingsService.GetSettingsAsync();

		if (settings.Garmin.Password != updatedGarminSettings.Password
			|| settings.Garmin.Email != updatedGarminSettings.Email)
			await _garminAuthService.SignOutAsync();

		settings.Garmin = updatedGarminSettings.Map();

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

		if (!string.IsNullOrWhiteSpace(updatedPelotonSettings.Password) 
			&& updatedPelotonSettings.Password.Contains('\\'))
		{
			result.Successful = false;
			result.Error = new ServiceError() { Message = "P2G does not support the `\\` character in passwords." };
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
