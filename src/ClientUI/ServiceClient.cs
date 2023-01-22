using Api.Contract;
using Api.Services;
using Common;
using Common.Service;
using SharedUI;

namespace ClientUI;

public class ServiceClient : IApiClient
{
	private readonly ISystemInfoService _systemInfoService;
	private readonly ISettingsService _settingsService;

	public ServiceClient(ISystemInfoService systemInfoService, ISettingsService settingsService)
	{
		_systemInfoService = systemInfoService;
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
		var result = await _systemInfoService.GetAsync(systemInfoGetRequest);
		result.Api = null;
		return result;
	}
}
