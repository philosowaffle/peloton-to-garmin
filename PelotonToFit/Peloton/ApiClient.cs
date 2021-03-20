using Common.Dto;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Peloton.Dto;
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

		public async Task InitAuthAsync()
		{
			if (!string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(SessionId))
				return;

			var response = await $"{AuthBaseUrl}"
				.WithHeader("peloton-platform", "web")
				.WithHeader("DNT", "1")
				.WithHeader("Accept-Language", "en-US")
				.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.90 Safari/537.36")
				.PostJsonAsync(new AuthRequest()
				{
					username_or_email = _userEmail,
					password = _userPassword
				})
				.ReceiveJson<AuthResponse>();

			UserId = response.user_id;
			SessionId = response.session_id;

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
