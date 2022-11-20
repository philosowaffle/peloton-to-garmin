using Common;
using Common.Dto.Api;
using Flurl;
using Flurl.Http;

namespace WebUI;

public interface IApiClient
{
	Task<PelotonWorkoutsGetResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetRequest request);
	Task<PelotonWorkoutsSinceGetResponse> PelotonWorkoutsGetAsync(PelotoWorkoutsSinceGetRequest request);

	Task<SettingsGetResponse> SettingsGetAsync();
	Task<Common.App> SettingsAppPostAsync(Common.App appSettings);
	Task<Format> SettingsFormatPostAsync(Format formatSettings);
	Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(Common.Peloton pelotonSettings);
	Task<SettingsGarminGetResponse> SettingsGarminPostAsync(Common.Garmin garminSettings);

	Task<SyncGetResponse> SyncGetAsync();
	Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest);

	Task<SystemInfoGetResponse> SystemInfoGetAsync(SystemInfoGetRequest systemInfoGetRequest);
}

public class ApiClient : IApiClient
{
	private string _apiUrl;

	public ApiClient(string apiUrl)
	{
		_apiUrl = apiUrl;
	}

	public Task<PelotonWorkoutsGetResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetRequest request)
	{
		return $"{_apiUrl}/api/peloton/workouts"
				.SetQueryParams(request)
				.GetJsonAsync<PelotonWorkoutsGetResponse>();
	}

	public Task<SettingsGetResponse> SettingsGetAsync()
	{
		return $"{_apiUrl}/api/settings"
				.GetJsonAsync<SettingsGetResponse>();
	}

	public Task<Common.App> SettingsAppPostAsync(Common.App appSettings)
	{
		return $"{_apiUrl}/api/settings/app"
				.PostJsonAsync(appSettings)
				.ReceiveJson<Common.App>();
	}

	public Task<Format> SettingsFormatPostAsync(Format formatSettings)
	{
		return $"{_apiUrl}/api/settings/format"
				.PostJsonAsync(formatSettings)
				.ReceiveJson<Format>();
	}
	
	public Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(Common.Peloton pelotonSettings)
	{
		return $"{_apiUrl}/api/settings/peloton"
				.PostJsonAsync(pelotonSettings)
				.ReceiveJson<SettingsPelotonGetResponse>();
	}

	public Task<SettingsGarminGetResponse> SettingsGarminPostAsync(Common.Garmin garminSettings)
	{
		return $"{_apiUrl}/api/settings/garmin"
				.PostJsonAsync(garminSettings)
				.ReceiveJson<SettingsGarminGetResponse>();
	}

	public Task<SyncGetResponse> SyncGetAsync()
	{
		return $"{_apiUrl}/api/sync"
				.GetJsonAsync<SyncGetResponse>();
	}

	public Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest)
	{
		return $"{_apiUrl}/api/sync"
				.PostJsonAsync(syncPostRequest)
				.ReceiveJson<SyncPostResponse>();
	}

	public Task<SystemInfoGetResponse> SystemInfoGetAsync(SystemInfoGetRequest systemInfoGetRequest)
	{
		return $"{_apiUrl}/api/systemInfo"
				.SetQueryParams(systemInfoGetRequest)
				.GetJsonAsync<SystemInfoGetResponse>();
	}

	public Task<PelotonWorkoutsSinceGetResponse> PelotonWorkoutsGetAsync(PelotoWorkoutsSinceGetRequest request)
	{
		return $"{_apiUrl}/api/peloton/workouts/sinceDate"
				.SetQueryParams(request)
				.GetJsonAsync<PelotonWorkoutsSinceGetResponse>();
	}
}
