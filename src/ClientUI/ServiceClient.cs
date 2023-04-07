﻿using Api.Contract;
using Api.Service;
using Api.Service.Helpers;
using Api.Service.Mappers;
using Api.Service.Validators;
using Api.Services;
using Common;
using Common.Database;
using Common.Dto.Peloton;
using Common.Service;
using Flurl.Http;
using Garmin.Auth;
using Peloton;
using Peloton.AnnualChallenge;
using Peloton.Dto;
using SharedUI;
using Sync;

namespace ClientUI;

public class ServiceClient : IApiClient
{
	private readonly ISystemInfoService _systemInfoService;
	private readonly ISettingsService _settingsService;
	private readonly ISettingsUpdaterService _settingsUpdaterService;
	private readonly IAnnualChallengeService _annualChallengeService;
	private readonly IPelotonService _pelotonService;
	private readonly IGarminAuthenticationService _garminAuthService;
	private readonly ISyncService _syncService;
	private readonly ISyncStatusDb _syncStatusDb;

	public ServiceClient(ISystemInfoService systemInfoService, ISettingsService settingsService, IAnnualChallengeService annualChallengeService, ISettingsUpdaterService settingsUpdaterService, IPelotonService pelotonService, IGarminAuthenticationService garminAuthService, ISyncService syncService, ISyncStatusDb syncStatusDb)
	{
		_systemInfoService = systemInfoService;
		_settingsService = settingsService;
		_annualChallengeService = annualChallengeService;
		_settingsUpdaterService = settingsUpdaterService;
		_pelotonService = pelotonService;
		_garminAuthService = garminAuthService;
		_syncService = syncService;
		_syncStatusDb = syncStatusDb;
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
			var tiers = data.Tiers?.Select(t => t.Map()).ToList();

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

	public async Task<GarminAuthenticationGetResponse> GetGarminAuthenticationAsync()
	{
		var settings = await _settingsService.GetSettingsAsync();
		var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);

		var result = new GarminAuthenticationGetResponse() { IsAuthenticated = auth?.IsValid(settings) ?? false };
		return result;
	}

	public async Task<PelotonWorkoutsGetResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetRequest request)
	{
		if (!request.IsValid(out var result))
			throw new ApiClientException(result);

		PagedPelotonResponse<Workout> recentWorkouts = null;

		try
		{
			recentWorkouts = await _pelotonService.GetPelotonWorkoutsAsync(request.PageSize, request.PageIndex);
		}
		catch (ArgumentException ae)
		{
			throw new ApiClientException(new ErrorResponse(ae.Message, ae));
		}
		catch (PelotonAuthenticationError pe)
		{
			throw new ApiClientException(new ErrorResponse(pe.Message, pe));
		}
		catch (Exception e)
		{
			throw new ApiClientException(new ErrorResponse($"Unexpected error occurred: {e.Message}", e));
		}

		return new PelotonWorkoutsGetResponse()
		{
			PageSize = recentWorkouts.Limit,
			PageIndex = recentWorkouts.Page,
			PageCount = recentWorkouts.Page_Count,
			TotalItems = recentWorkouts.Total,
			Items = recentWorkouts.data
					.OrderByDescending(i => i.Created_At)
					.Select(w => new PelotonWorkout(w))
					.ToList()
		};
	}

	public Task<PelotonWorkoutsGetAllResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetAllRequest request)
	{
		throw new NotImplementedException();
	}

	public async Task SendGarminMfaTokenAsync(GarminAuthenticationMfaTokenPostRequest request)
	{
		var settings = await _settingsService.GetSettingsAsync();

		if (!settings.Garmin.TwoStepVerificationEnabled)
			throw new ApiClientException(new ErrorResponse("Garmin two step verification is not enabled in Settings."));

		try
		{
			await _garminAuthService.CompleteMFAAuthAsync(request.MfaToken);
			return;
		}
		catch (Exception e)
		{
			throw new ApiClientException(new ErrorResponse($"Unexpected error occurred: {e.Message}", e));
		}
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

	public async Task<Format> SettingsFormatPostAsync(Format formatSettings)
	{
		try
		{
			var result = await _settingsUpdaterService.UpdateFormatSettingsAsync(formatSettings);

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return result.Result;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
	}

	public async Task<SettingsGarminGetResponse> SettingsGarminPostAsync(SettingsGarminPostRequest garminSettings)
	{
		try
		{
			var result = await _settingsUpdaterService.UpdateGarminSettingsAsync(garminSettings);

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return result.Result;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
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

	public async Task<IFlurlResponse> SignInToGarminAsync()
	{
		var settings = await _settingsService.GetSettingsAsync();

		if (settings.Garmin.Password.CheckIsNullOrEmpty("Garmin Password", out var result)) throw new ApiClientException(result);
		if (settings.Garmin.Email.CheckIsNullOrEmpty("Garmin Email", out result)) throw new ApiClientException(result);

		try
		{
			if (!settings.Garmin.TwoStepVerificationEnabled)
			{
				await _garminAuthService.RefreshGarminAuthenticationAsync();
				return new FlurlResponse(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Created });
			}
			else
			{
				var auth = await _garminAuthService.RefreshGarminAuthenticationAsync();

				if (auth.AuthStage == Common.Stateful.AuthStage.NeedMfaToken)
					return new FlurlResponse(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Accepted });

				return new FlurlResponse(new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Created });
			}
		}
		catch (GarminAuthenticationError gae) when (gae.Code == Code.UnexpectedMfa)
		{
			throw new ApiClientException(new ErrorResponse("It looks like your account is protected by two step verification. Please enable the Two Step verification setting.", ErrorCode.UnexpectedGarminMFA, gae));
		}
		catch (GarminAuthenticationError gae) when (gae.Code == Code.InvalidCredentials)
		{
			throw new ApiClientException(new ErrorResponse("Garmin authentication failed. Invalid Garmin credentials.", ErrorCode.InvalidGarminCredentials, gae));
		}
		catch (Exception e)
		{
			throw new ApiClientException(new ErrorResponse($"Unexpected error occurred: {e.Message}", e));
		}
	}

	public async Task<SyncGetResponse> SyncGetAsync()
	{
		var syncTimeTask = _syncStatusDb.GetSyncStatusAsync();
		var settingsTask = _settingsService.GetSettingsAsync();

		await Task.WhenAll(syncTimeTask, settingsTask);

		var syncTime = await syncTimeTask;
		var settings = await settingsTask;

		return new SyncGetResponse()
		{
			SyncEnabled = settings.App.EnablePolling,
			SyncStatus = syncTime.SyncStatus,
			LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
			LastSyncTime = syncTime.LastSyncTime,
			NextSyncTime = syncTime.NextSyncTime
		};
	}

	public async Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest)
	{
		var settings = await _settingsService.GetSettingsAsync();
		var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);

		var (isValid, result) = syncPostRequest.IsValid(settings, auth);
		if (!isValid)
			throw new ApiClientException(result);

		SyncResult syncResult = new();
		try
		{
			syncResult = await _syncService.SyncAsync(syncPostRequest.WorkoutIds, exclude: null);
		}
		catch (Exception e)
		{
			throw new ApiClientException(new ErrorResponse($"Unexpected error occurred: {e.Message}", e));
		}

		return new SyncPostResponse()
		{
			SyncSuccess = syncResult.SyncSuccess,
			PelotonDownloadSuccess = syncResult.PelotonDownloadSuccess,
			ConverToFitSuccess = syncResult.ConversionSuccess,
			UploadToGarminSuccess = syncResult.UploadToGarminSuccess,
			Errors = syncResult.Errors.Select(e => new ErrorResponse(e.Message)).ToList()
		};
	}

	public async Task<SystemInfoGetResponse> SystemInfoGetAsync(SystemInfoGetRequest systemInfoGetRequest)
	{
		var result = await _systemInfoService.GetAsync(systemInfoGetRequest);
		result.Api = null;
		return result;
	}
}
