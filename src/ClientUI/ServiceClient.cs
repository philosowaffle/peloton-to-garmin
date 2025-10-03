using Api.Contract;
using Api.Service;
using Api.Service.Helpers;
using Api.Service.Validators;
using Api.Services;
using Common.Dto.Peloton;
using Common.Dto;
using Common.Service;
using Flurl.Http;
using Garmin.Auth;
using Peloton;
using Peloton.Dto;
using SharedUI;
using Sync;
using Garmin.Dto;
using Sync.Database;
using Microsoft.Extensions.Logging;

namespace ClientUI;

public class ServiceClient : IApiClient
{
	private readonly ISystemInfoService _systemInfoService;
	private readonly ISettingsService _settingsService;
	private readonly ISettingsUpdaterService _settingsUpdaterService;
	private readonly IPelotonAnnualChallengeService _annualChallengeService;
	private readonly IPelotonService _pelotonService;
	private readonly IGarminAuthenticationService _garminAuthService;
	private readonly ISyncService _syncService;
	private readonly ISyncStatusDb _syncStatusDb;

	public ServiceClient(ISystemInfoService systemInfoService, ISettingsService settingsService, IPelotonAnnualChallengeService annualChallengeService, ISettingsUpdaterService settingsUpdaterService, IPelotonService pelotonService, IGarminAuthenticationService garminAuthService, ISyncService syncService, ISyncStatusDb syncStatusDb)
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
		try
		{
			var result = await _annualChallengeService.GetProgressAsync();

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return result.Result;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
	}

	public async Task<GarminAuthenticationGetResponse> GetGarminAuthenticationAsync()
	{
		var auth = await _garminAuthService.GetGarminAuthenticationAsync();

		var result = new GarminAuthenticationGetResponse() { IsAuthenticated = auth?.IsValid() ?? false };
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

	public async Task<PelotonWorkoutsGetAllResponse> PelotonWorkoutsGetAsync(PelotonWorkoutsGetAllRequest request)
	{
		if (request.SinceDate.IsAfter(DateTime.UtcNow, nameof(request.SinceDate), out var result))
			throw new ApiClientException(result!);

		ICollection<Workout> workoutsToReturn = new List<Workout>();
		var completedOnly = request.WorkoutStatusFilter == WorkoutStatus.Completed;

		try
		{
			var serviceResult = await _pelotonService.GetWorkoutsSinceAsync(request.SinceDate);

			if (serviceResult.IsErrored())
				throw new ApiClientException(new ErrorResponse(serviceResult.Error.Message));

			foreach (var w in serviceResult.Result)
			{
				if (completedOnly && w.Status != "COMPLETE")
					continue;

				if (request.ExcludeWorkoutTypes.Contains(w.GetWorkoutType()))
					continue;

				workoutsToReturn.Add(w);
			}
		}
		catch (Exception e)
		{
			throw new ApiClientException(new ErrorResponse($"Unexpected error occurred: {e.Message}", e));
		}

		return new PelotonWorkoutsGetAllResponse()
		{
			SinceDate = request.SinceDate,
			Items = workoutsToReturn
					.OrderByDescending(i => i.Created_At)
					.Select(w => new PelotonWorkout(w))
					.ToList()
		};
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

	public async Task<Common.Dto.App> SettingsAppPostAsync(Common.Dto.App appSettings)
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
		var result = await _settingsUpdaterService.UpdateGarminSettingsAsync(garminSettings);

		if (result.IsErrored())
			throw new ApiClientException(result.Error.Message, result.Error.Exception);

		return result.Result;
	}

	public async Task<SettingsPelotonGetResponse> SettingsPelotonPostAsync(SettingsPelotonPostRequest pelotonSettings)
	{
		var result = await _settingsUpdaterService.UpdatePelotonSettingsAsync(pelotonSettings);

		if (result.IsErrored())
			throw new ApiClientException(result.Error.Message, result.Error.Exception);

		return result.Result;
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
				await _garminAuthService.SignInAsync();
				return new FlurlResponse(new FlurlCall() { HttpResponseMessage = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Created } });
			}
			else
			{
				var auth = await _garminAuthService.SignInAsync();

				if (auth.AuthStage == AuthStage.NeedMfaToken)
					return new FlurlResponse(new FlurlCall() { HttpResponseMessage = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Accepted } });

				return new FlurlResponse(new FlurlCall() { HttpResponseMessage = new HttpResponseMessage() { StatusCode = System.Net.HttpStatusCode.Created } });
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
			SyncStatus = (Status)syncTime.SyncStatus,
			LastSuccessfulSyncTime = syncTime.LastSuccessfulSyncTime,
			LastSyncTime = syncTime.LastSyncTime,
			NextSyncTime = syncTime.NextSyncTime
		};
	}

	public async Task<SyncPostResponse> SyncPostAsync(SyncPostRequest syncPostRequest)
	{
		var settings = await _settingsService.GetSettingsAsync();
		var auth = await _garminAuthService.GetGarminAuthenticationAsync();

		var (isValid, result) = syncPostRequest.IsValid(settings, auth);
		if (!isValid)
			throw new ApiClientException(result);

		SyncResult syncResult = new();
		try
		{
			syncResult = await _syncService.SyncAsync(syncPostRequest.WorkoutIds, exclude: null, forceStackWorkouts: syncPostRequest.ForceStackWorkouts);
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

	public async Task<SystemInfoLogsGetResponse> SystemInfoGetLogsAsync()
	{
		try
		{
			var result = await _systemInfoService.GetLogsAsync();

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return result.Result;
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
	}

	public async Task<LogLevelPostResponse> LogLevelPostAsync(LogLevelPostRequest request)
	{
		try
		{
			var result = await _systemInfoService.SetLogLevelAsync(request);

			if (result.IsErrored())
				throw new ApiClientException(result.Error.Message, result.Error.Exception);

			return new LogLevelPostResponse() { LogLevel = result.Result };
		}
		catch (Exception e)
		{
			throw new ApiClientException($"Unexpected error occurred: {e.Message}", e);
		}
	}
}
