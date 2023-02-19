using Api.Contract;
using Api.Service;
using Api.Service.Helpers;
using Api.Services;
using Common;
using Common.Dto;
using Common.Service;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using Peloton.AnnualChallenge;
using SharedUI;

namespace ClientUI;

public class ServiceClient : IApiClient
{
	private readonly ISystemInfoService _systemInfoService;
	private readonly ISettingsService _settingsService;
	private readonly ISettingsUpdaterService _settingsUpdaterService;
	private readonly IAnnualChallengeService _annualChallengeService;

	public ServiceClient(ISystemInfoService systemInfoService, ISettingsService settingsService, IAnnualChallengeService annualChallengeService, ISettingsUpdaterService settingsUpdaterService)
	{
		_systemInfoService = systemInfoService;
		_settingsService = settingsService;
		_annualChallengeService = annualChallengeService;
		_settingsUpdaterService = settingsUpdaterService;
	}

	public async Task<ProgressGetResponse> GetAnnualProgressAsync()
	{
		var userId = 1;
		try
		{
			var serviceResult = await _annualChallengeService.GetAnnualChallengeProgressAsync(userId);

			if (serviceResult.IsErrored())
				throw new ApiClientException(serviceResult.Error.Message, serviceResult.Error.Exception);

			var data = serviceResult.Result;
			var tiers = data.Tiers?.Select(t => new Api.Contract.Tier()
			{
				BadgeUrl = t.BadgeUrl,
				Title = t.Title,
				RequiredMinutes = t.RequiredMinutes,
				HasEarned = t.HasEarned,
				PercentComplete = Convert.ToSingle(t.PercentComplete * 100),
				IsOnTrackToEarndByEndOfYear = t.IsOnTrackToEarndByEndOfYear,
				MinutesBehindPace = t.MinutesBehindPace,
				MinutesAheadOfPace = t.MinutesAheadOfPace,
				MinutesNeededPerDay = t.MinutesNeededPerDay,
				MinutesNeededPerWeek = t.MinutesNeededPerWeek,
			}).ToList();

			return new ProgressGetResponse()
			{
				EarnedMinutes = data.EarnedMinutes,
				Tiers = tiers ?? new List<Api.Contract.Tier>(),
			};
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error ocurred: {e.Message}", e);
		}
	}

	public Task<GarminAuthenticationGetResponse> GetGarminAuthenticationAsync()
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

	public Task SendGarminMfaTokenAsync(GarminAuthenticationMfaTokenPostRequest request)
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

	public async Task<Common.App> SettingsAppPostAsync(Common.App appSettings)
	{
		try
		{
			var result = await _settingsUpdaterService.UpdateAppSettingsAsync(appSettings);

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return result.Result;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
	}

	public Task<Format> SettingsFormatPostAsync(Format formatSettings)
	{
		throw new NotImplementedException();
	}

	public Task<SettingsGarminGetResponse> SettingsGarminPostAsync(SettingsGarminPostRequest garminSettings)
	{
		throw new NotImplementedException();
	}

	public async Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(SettingsPelotonPostRequest pelotonSettings)
	{
		try
		{
			var result = await _settingsUpdaterService.UpdatePelotonSettingsAsync(pelotonSettings);

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return result.Result;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
	}

	public Task<IFlurlResponse> SignInToGarminAsync()
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
