using Common.Dto;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Peloton.Dto;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Peloton
{
	public class ApiClient
	{
		private static readonly string BaseUrl = "https://api.onepeloton.com/api";
		private static readonly string AuthBaseUrl = "https://api.onepeloton.com/auth/login";

		private readonly string _userEmail;
		private readonly string _userPassword;

		private string UserId;
		private string SessionId;

		public ApiClient(string email, string password)
		{
			_userEmail = email;
			_userPassword = password;
		}

		public async Task InitAuthAsync(string overrideUserAgent = null)
		{
			if (!string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(SessionId))
				return;

			try
			{
				var response = await $"{AuthBaseUrl}"
				.WithHeader("Accept-Language", "en-US")
				.WithHeader("User-Agent", overrideUserAgent ?? "PostmanRuntime/7.26.10")
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
				Log.Error(e, "Failed to authenticate with Peloton.");
				throw;
			}
		}

		public Task<RecentWorkouts> GetWorkoutsAsync(int numWorkouts)
		{
			return $"{BaseUrl}/user/{UserId}/workouts"
			.WithCookie("peloton_session_id", SessionId)
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created"
			})
			.GetJsonAsync<RecentWorkouts>();
		}

		public Task<JObject> GetWorkoutByIdAsync(string id)
		{
			return $"{BaseUrl}/workout/{id}"
				.WithCookie("peloton_session_id", SessionId)
				.SetQueryParams(new
				{
					joins = "ride,ride.instructor"
				})
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
				.GetJsonAsync<JObject>();
		}

		public Task<JObject> GetWorkoutSummaryByIdAsync(string id)
		{
			return $"{BaseUrl}/workout/{id}/summary"
				.WithCookie("peloton_session_id", SessionId)
				.GetJsonAsync<JObject>();
		}
	}
}
