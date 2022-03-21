using Common;
using Flurl.Http;
using WebUI.Domain;

namespace WebUI
{
	public interface IApiClient
	{
		Task<Settings> SettingsGetAsync();
		Task<Common.App> SettingsAppPostAsync(Common.App appSettings);
		Task<Format> SettingsFormatPostAsync(Format formatSettings);
		Task<Peloton> SettingsPelotonPostAsync(Peloton pelotonSettings);
		Task<Garmin> SettingsGarminPostAsync(Garmin garminSettings);

		Task<SyncGetResponse> SyncGetAsync();
		Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest);

		Task<SystemInfoGetResponse> SystemInfoGetAsync();
	}

	public class ApiClient : IApiClient
	{
		private string _apiUrl;

		public ApiClient(string apiUrl)
		{
			_apiUrl = apiUrl;
		}

		public Task<Settings> SettingsGetAsync()
		{
			return $"{_apiUrl}/api/settings"
					.GetJsonAsync<Settings>();
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
		
		public Task<Peloton> SettingsPelotonPostAsync(Peloton pelotonSettings)
		{
			return $"{_apiUrl}/api/settings/peloton"
					.PostJsonAsync(pelotonSettings)
					.ReceiveJson<Peloton>();
		}

		public Task<Garmin> SettingsGarminPostAsync(Garmin garminSettings)
		{
			return $"{_apiUrl}/api/settings/garmin"
					.PostJsonAsync(garminSettings)
					.ReceiveJson<Garmin>();
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

		public Task<SystemInfoGetResponse> SystemInfoGetAsync()
		{
			return $"{_apiUrl}/api/systemInfo"
					.GetJsonAsync<SystemInfoGetResponse>();
		}
	}
}
