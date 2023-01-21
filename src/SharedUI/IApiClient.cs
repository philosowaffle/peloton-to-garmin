using Common;
using Common.Dto.Api;

namespace SharedUI;
public interface IApiClient
{
	Task<PelotonWorkoutsGetResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetRequest request);
	Task<PelotonWorkoutsGetAllResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetAllRequest request);

	Task<SettingsGetResponse> SettingsGetAsync();
	Task<Common.App> SettingsAppPostAsync(Common.App appSettings);
	Task<Format> SettingsFormatPostAsync(Format formatSettings);
	Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(SettingsPelotonPostRequest pelotonSettings);
	Task<SettingsGarminGetResponse> SettingsGarminPostAsync(SettingsGarminPostRequest garminSettings);

	Task<SyncGetResponse> SyncGetAsync();
	Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest);

	Task<SystemInfoGetResponse> SystemInfoGetAsync(SystemInfoGetRequest systemInfoGetRequest);

	Task<ProgressGetResponse> GetAnnualProgressAsync();
}