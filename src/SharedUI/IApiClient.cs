﻿using Api.Contract;
using Common;
using Flurl.Http;

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

	Task<GarminAuthenticationGetResponse> GetGarminAuthenticationAsync();
	Task<IFlurlResponse> SignInToGarminAsync();
	Task SendGarminMfaTokenAsync(GarminAuthenticationMfaTokenPostRequest request);
}

public class ApiClientException : Exception
{
	public ApiClientException(string? message, Exception innerException) : base(message, innerException) { }
	public ApiClientException(ErrorResponse error) : base(error.Message, error.Exception) { }
}

public class SyncTimeoutException : Exception
{
	public SyncTimeoutException(string? message, Exception innerException) : base(message, innerException) { }
}