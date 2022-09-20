using Common;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Peloton.Dto;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Peloton
{
	public interface IPelotonApi
	{
		Task<RecentWorkouts> GetWorkoutsAsync(int pageSize, int page);
		Task<JObject> GetWorkoutByIdAsync(string id);
		Task<JObject> GetWorkoutSamplesByIdAsync(string id);
		Task<UserData> GetUserDataAsync();
	}

	public class ApiClient : IPelotonApi
	{
		private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();
		private static readonly string BaseUrl = "https://api.onepeloton.com/api";
		private static readonly string AuthBaseUrl = "https://api.onepeloton.com/auth/login";

		private readonly ISettingsService _settingsService;

		public ApiClient(ISettingsService settingsService)
		{
			_settingsService = settingsService;
		}

		public async Task<PelotonApiAuthentication> GetAuthAsync(string overrideUserAgent = null)
		{
			var settings = await _settingsService.GetSettingsAsync();

			if (string.IsNullOrEmpty(settings.Peloton.Email))
				throw new ArgumentException("Peloton email is not set and is required.");

			if (string.IsNullOrEmpty(settings.Peloton.Password))
				throw new ArgumentException("Peloton password is not set and is required.");

			var auth = _settingsService.GetPelotonApiAuthentication(settings.Peloton.Email);
			if (auth is null) auth = new ();

			auth.Email = settings.Peloton.Email;
			auth.Password = settings.Peloton.Password;

			if (!string.IsNullOrEmpty(auth.UserId) && !string.IsNullOrEmpty(auth.SessionId))
				return auth;

			try
			{
				var response = await $"{AuthBaseUrl}"
				.WithHeader("Accept-Language", "en-US")
				.WithHeader("User-Agent", overrideUserAgent ?? "PostmanRuntime/7.26.10")
				.StripSensitiveDataFromLogging(auth.Email, auth.Password)
				.PostJsonAsync(new AuthRequest()
				{
					username_or_email = auth.Email,
					password = auth.Password
				})
				.ReceiveJson<AuthResponse>();

				auth.UserId = response.user_id;
				auth.SessionId = response.session_id;

				_settingsService.SetPelotonApiAuthentication(auth);
				return auth;
			}
			catch (FlurlHttpException fe) when (fe.StatusCode == (int)HttpStatusCode.Unauthorized)
			{
				_logger.Error(fe, $"Failed to authenticate with Peloton.");
				_settingsService.ClearPelotonApiAuthentication(auth.Email);
				throw new PelotonAuthenticationError("Failed to authenticate with Peloton. Please confirm your Peloton Email and Password are correct.", fe);
			}
			catch (Exception e)
			{
				_logger.Fatal(e, $"Failed to authenticate with Peloton.");
				_settingsService.ClearPelotonApiAuthentication(auth.Email);
				throw;
			}
		}

		public async Task<RecentWorkouts> GetWorkoutsAsync(int pageSize, int page)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{auth.UserId}/workouts"
			.WithCookie("peloton_session_id", auth.SessionId)
			.SetQueryParams(new
			{
				limit = pageSize,
				sort_by = "-created",
				page = page,
				joins= "ride"
			})
			.StripSensitiveDataFromLogging(auth.Email, auth.Password)
			.GetJsonAsync<RecentWorkouts>();
		}

		/// <summary>
		/// For ad hoc testing.
		/// </summary>
		public async Task<JObject> GetWorkoutsAsync(string userId, int numWorkouts, int page)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{userId}/workouts"
			.WithCookie("peloton_session_id", auth.SessionId)
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created",
				page = page,
				joins = "ride"
			})
			.StripSensitiveDataFromLogging(auth.Email, auth.Password)
			.GetJsonAsync<JObject>();
		}

		public async Task<UserData> GetUserDataAsync()
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/me"
			.WithCookie("peloton_session_id", auth.SessionId)
			.StripSensitiveDataFromLogging(auth.Email, auth.Password)
			.GetJsonAsync<UserData>();
		}

		public async Task<JObject> GetWorkoutByIdAsync(string id)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/workout/{id}"
				.WithCookie("peloton_session_id", auth.SessionId)
				.SetQueryParams(new
				{
					joins = "ride,ride.instructor"
				})
				.StripSensitiveDataFromLogging(auth.Email, auth.Password)
				.GetJsonAsync<JObject>();
		}

		public async Task<JObject> GetWorkoutSamplesByIdAsync(string id)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/workout/{id}/performance_graph"
				.WithCookie("peloton_session_id", auth.SessionId)
				.SetQueryParams(new
				{
					every_n=1
				})
				.StripSensitiveDataFromLogging(auth.Email, auth.Password)
				.GetJsonAsync<JObject>();
		}
	}
}
