using Common;
using Flurl.Http;
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
			using var tracer = Tracing.Source.StartActivity(nameof(InitAuthAsync))?
				.SetTag(Tracing.Category, Tracing.Http)?
				.SetTag(Tracing.Route, AuthBaseUrl)?
				.SetTag(Tracing.App, "peloton");

			var response = await $"{AuthBaseUrl}"
				.WithHeader("peloton-platform", "web")
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
			using var tracer = Tracing.Source.StartActivity(nameof(GetWorkoutsAsync))?
				.SetTag(Tracing.Category, Tracing.Http)?
				.SetTag(Tracing.Route, $"{BaseUrl}/user/{UserId}/workouts")?
				.SetTag(Tracing.App, "peloton");

			return $"{BaseUrl}/user/{UserId}/workouts"
			.WithCookie("peloton_session_id", SessionId)
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created"
			})
			.GetJsonAsync<RecentWorkouts>();
		}

		public Task<Workout> GetWorkoutByIdAsync(string id)
		{
			using var tracer = Tracing.Source.StartActivity(nameof(GetWorkoutByIdAsync))?
				.SetTag(Tracing.Category, Tracing.Http)?
				.SetTag(Tracing.Route, $"{BaseUrl}/workout/{id}")?
				.SetTag(Tracing.WorkoutId, id)?
				.SetTag(Tracing.App, "peloton");

			return $"{BaseUrl}/workout/{id}"
				.WithCookie("peloton_session_id", SessionId)
				.SetQueryParams(new
				{
					joins = "ride,ride.instructor"
				})
				.GetJsonAsync<Workout>();
		}

		public Task<WorkoutSamples> GetWorkoutSamplesByIdAsync(string id)
		{
			using var tracer = Tracing.Source.StartActivity(nameof(GetWorkoutSamplesByIdAsync))?
				.SetTag(Tracing.Category, Tracing.Http)?
				.SetTag(Tracing.Route, $"{BaseUrl}/workout/{id}/performance_graph")?
				.SetTag(Tracing.WorkoutId, id)?
				.SetTag(Tracing.App, "peloton");

			return $"{BaseUrl}/workout/{id}/performance_graph"
				.WithCookie("peloton_session_id", SessionId)
				.SetQueryParams(new
				{
					every_n=1
				})
				.GetJsonAsync<WorkoutSamples>();
		}

		public Task<WorkoutSummary> GetWorkoutSummaryByIdAsync(string id)
		{
			using var tracer = Tracing.Source.StartActivity(nameof(GetWorkoutSummaryByIdAsync))?
				.SetTag(Tracing.Category, Tracing.Http)?
				.SetTag(Tracing.Route, $"{BaseUrl}/workout/{id}/summary")?
				.SetTag(Tracing.WorkoutId, id)?
				.SetTag(Tracing.App, "peloton");

			return $"{BaseUrl}/workout/{id}/summary"
				.WithCookie("peloton_session_id", SessionId)
				.GetJsonAsync<WorkoutSummary>();
		}
	}
}
