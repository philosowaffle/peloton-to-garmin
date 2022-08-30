using Common;
using Common.Dto.Peloton;
using Common.Observe;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Peloton.Dto;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Peloton
{
	public interface IPelotonApi
	{
		Task InitAuthAsync(string overrideUserAgent = null);
		Task<RecentWorkouts> GetWorkoutsAsync(int numWorkouts, int page);
		Task<JObject> GetWorkoutByIdAsync(string id);
		Task<JObject> GetWorkoutSamplesByIdAsync(string id);
		Task<UserData> GetUserDataAsync();
	}

	public class ApiClient : IPelotonApi
	{
		private static readonly ILogger _logger = LogContext.ForClass<ApiClient>();
		private static readonly string BaseUrl = "https://api.onepeloton.com/api";
		private static readonly string AuthBaseUrl = "https://api.onepeloton.com/auth/login";

		private readonly string _userEmail;
		private readonly string _userPassword;

		private string UserId;
		private string SessionId;

		public ApiClient(Settings config, AppConfiguration appConfig)
		{
			_userEmail = config.Peloton.Email;
			_userPassword = config.Peloton.Password;
		}

		public ApiClient(string email, string password)
		{
			_userEmail = email;
			_userPassword = password;
		}

		public async Task InitAuthAsync(string overrideUserAgent = null)
		{
			if (!string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(SessionId))
				return;

			if (string.IsNullOrEmpty(_userEmail))
				throw new ArgumentException("Peloton email is not set and is required.");

			if (string.IsNullOrEmpty(_userPassword))
				throw new ArgumentException("Peloton password is not set and is required.");

			try
			{
				var response = await $"{AuthBaseUrl}"
				.WithHeader("Accept-Language", "en-US")
				.WithHeader("User-Agent", overrideUserAgent ?? "PostmanRuntime/7.26.10")
				.StripSensitiveDataFromLogging(_userEmail, _userPassword)
				.PostJsonAsync(new AuthRequest()
				{
					username_or_email = _userEmail,
					password = _userPassword
				})
				.ReceiveJson<AuthResponse>();

				UserId = response.user_id;
				SessionId = response.session_id;
			} catch(Exception e)
			{
				_logger.Fatal(e, "Failed to authenticate with Peloton. Email: {@Email}", _userEmail);
				throw new PelotonAuthenticationError("Failed to authenticate with Peloton", e);
			}
		}

		public Task<RecentWorkouts> GetWorkoutsAsync(int numWorkouts, int page)
		{
			return $"{BaseUrl}/user/{UserId}/workouts"
			.WithCookie("peloton_session_id", SessionId)
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created",
				page = page,
				joins= "ride"
			})
			.StripSensitiveDataFromLogging(_userEmail, _userPassword)
			.GetJsonAsync<RecentWorkouts>();
		}

		/// <summary>
		/// For ad hoc testing.
		/// </summary>
		public Task<JObject> GetWorkoutsAsync(string userId, int numWorkouts, int page)
		{
			return $"{BaseUrl}/user/{userId}/workouts"
			.WithCookie("peloton_session_id", SessionId)
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created",
				page = page,
				joins = "ride"
			})
			.StripSensitiveDataFromLogging(_userEmail, _userPassword)
			.GetJsonAsync<JObject>();
		}

		public Task<UserData> GetUserDataAsync()
		{
			return $"{BaseUrl}/me"
			.WithCookie("peloton_session_id", SessionId)
			.StripSensitiveDataFromLogging(_userEmail, _userPassword)
			.GetJsonAsync<UserData>();
		}

		public Task<JObject> GetWorkoutByIdAsync(string id)
		{
			return $"{BaseUrl}/workout/{id}"
				.WithCookie("peloton_session_id", SessionId)
				.SetQueryParams(new
				{
					joins = "ride,ride.instructor"
				})
				.StripSensitiveDataFromLogging(_userEmail, _userPassword)
				.GetJsonAsync<JObject>();
		}

		public Task<JObject> GetWorkoutSamplesByIdAsync(string id)
		{
			return $"{BaseUrl}/workout/{id}/performance_graph"
				.WithCookie("peloton_session_id", SessionId)
				.SetQueryParams(new
				{
					every_n=1
				})
				.StripSensitiveDataFromLogging(_userEmail, _userPassword)
				.GetJsonAsync<JObject>();
		}
	}
}
