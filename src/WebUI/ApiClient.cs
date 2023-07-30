﻿using Api.Contract;
using Common;
using Flurl;
using Flurl.Http;
using SharedUI;

namespace WebUI;

public class ApiClient : IApiClient
{
	private string _apiUrl;

	public ApiClient(string apiUrl)
	{
		_apiUrl = apiUrl;
	}

	public async Task<PelotonWorkoutsGetResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetRequest request)
	{
		try
		{
			return await $"{_apiUrl}/api/peloton/workouts"
						.SetQueryParams(request)
						.GetJsonAsync<PelotonWorkoutsGetResponse>();

		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<SettingsGetResponse> SettingsGetAsync()
	{
		try
		{
			return await $"{_apiUrl}/api/settings"
				.GetJsonAsync<SettingsGetResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<Common.App> SettingsAppPostAsync(Common.App appSettings)
	{
		try
		{
			return await $"{_apiUrl}/api/settings/app"
				.PostJsonAsync(appSettings)
				.ReceiveJson<Common.App>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<Format> SettingsFormatPostAsync(Format formatSettings)
	{
		try
		{
			return await $"{_apiUrl}/api/settings/format"
				.PostJsonAsync(formatSettings)
				.ReceiveJson<Format>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(SettingsPelotonPostRequest pelotonSettings)
	{
		try
		{
			return await $"{_apiUrl}/api/settings/peloton"
				.PostJsonAsync(pelotonSettings)
				.ReceiveJson<SettingsPelotonGetResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<SettingsGarminGetResponse> SettingsGarminPostAsync(SettingsGarminPostRequest garminSettings)
	{
		try
		{
			return await $"{_apiUrl}/api/settings/garmin"
				.PostJsonAsync(garminSettings)
				.ReceiveJson<SettingsGarminGetResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<SyncGetResponse> SyncGetAsync()
	{
		try
		{
			return await $"{_apiUrl}/api/sync"
				.GetJsonAsync<SyncGetResponse>();
		}
		catch (FlurlHttpTimeoutException te)
		{
			throw new SyncTimeoutException(te.Message, te);
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest)
	{
		try
		{
			return await $"{_apiUrl}/api/sync"
				.PostJsonAsync(syncPostRequest)
				.ReceiveJson<SyncPostResponse>();
		}
		catch (FlurlHttpTimeoutException te)
		{
			throw new SyncTimeoutException(te.Message, te);
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<SystemInfoGetResponse> SystemInfoGetAsync(SystemInfoGetRequest systemInfoGetRequest)
	{
		try
		{
			return await $"{_apiUrl}/api/systemInfo"
				.SetQueryParams(systemInfoGetRequest)
				.GetJsonAsync<SystemInfoGetResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<PelotonWorkoutsGetAllResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetAllRequest request)
	{
		try
		{
			return await $"{_apiUrl}/api/peloton/workouts/all"
				.SetQueryParams(request)
				.GetJsonAsync<PelotonWorkoutsGetAllResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public async Task<ProgressGetResponse> GetAnnualProgressAsync()
	{
		try
		{
			return await $"{_apiUrl}/api/pelotonannualchallenge/progress"
				.GetJsonAsync<ProgressGetResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}

	public Task<GarminAuthenticationGetResponse> GetGarminAuthenticationAsync()
	{
		return $"{_apiUrl}/api/garminauthentication"
				.GetJsonAsync<GarminAuthenticationGetResponse>();
	}

	public Task<IFlurlResponse> SignInToGarminAsync()
	{
		return $"{_apiUrl}/api/garminauthentication/signin"
			.PostAsync();
	}

	public Task SendGarminMfaTokenAsync(GarminAuthenticationMfaTokenPostRequest request)
	{
		return $"{_apiUrl}/api/garminauthentication/mfaToken"
			.PostJsonAsync(request);
	}

	public async Task<SystemInfoLogsGetResponse> SystemInfoGetLogsAsync()
	{
		try
		{
			return await $"{_apiUrl}/api/systemInfo/logs"
				.GetJsonAsync<SystemInfoLogsGetResponse>();
		}
		catch (FlurlHttpException e)
		{
			var error = await e.GetResponseJsonAsync<ErrorResponse>();
			throw new ApiClientException(error?.Message, e);
		}
	}
}
