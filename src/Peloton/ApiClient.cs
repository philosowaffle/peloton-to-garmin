using Common;
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
		private readonly bool _observabilityEnabled;

		private string UserId;
		private string SessionId;

		public ApiClient(string email, string password, bool observabilityEnabled)
		{
			_userEmail = email;
			_userPassword = password;
			_observabilityEnabled = observabilityEnabled;
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
			.ConfigureRequest((c) => 
			{
				c.AfterCallAsync = async (FlurlCall call) => 
				{
					if (_observabilityEnabled)
					{
						FlurlConfiguration.HttpReqeustHistogram
						.WithLabels(
							call.HttpRequestMessage.Method.ToString(),
							call.HttpRequestMessage.RequestUri.Host,
							"/user/{userid}/workouts",
							"?limit={limit}&sort_by={sortby}",
							((int)call.HttpResponseMessage.StatusCode).ToString(),
							call.HttpResponseMessage.ReasonPhrase
						).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
					}
				};
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
				.ConfigureRequest((c) =>
				{
					c.AfterCallAsync = async (FlurlCall call) =>
					{
						if (_observabilityEnabled)
						{
							FlurlConfiguration.HttpReqeustHistogram
							.WithLabels(
								call.HttpRequestMessage.Method.ToString(),
								call.HttpRequestMessage.RequestUri.Host,
								"/workout/{workoutid}",
								"?joins={joins}",
								((int)call.HttpResponseMessage.StatusCode).ToString(),
								call.HttpResponseMessage.ReasonPhrase
							).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
						}
					};
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
				.ConfigureRequest((c) =>
				{
					c.AfterCallAsync = async (FlurlCall call) =>
					{
						if (_observabilityEnabled)
						{
							FlurlConfiguration.HttpReqeustHistogram
							.WithLabels(
								call.HttpRequestMessage.Method.ToString(),
								call.HttpRequestMessage.RequestUri.Host,
								"/workout/{workoutid}/performance_graph",
								"?every_n={everyn}",
								((int)call.HttpResponseMessage.StatusCode).ToString(),
								call.HttpResponseMessage.ReasonPhrase
							).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
						}
					};
				})
				.GetJsonAsync<JObject>();
		}

		public Task<JObject> GetWorkoutSummaryByIdAsync(string id)
		{
			return $"{BaseUrl}/workout/{id}/summary"
				.WithCookie("peloton_session_id", SessionId)
				.ConfigureRequest((c) =>
				{
					c.AfterCallAsync = async (FlurlCall call) =>
					{
						if (_observabilityEnabled)
						{
							FlurlConfiguration.HttpReqeustHistogram
							.WithLabels(
								call.HttpRequestMessage.Method.ToString(),
								call.HttpRequestMessage.RequestUri.Host,
								"/workout/{workoutid}/summary",
								call.HttpRequestMessage.RequestUri.Query,
								((int)call.HttpResponseMessage.StatusCode).ToString(),
								call.HttpResponseMessage.ReasonPhrase
							).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
						}
					};
				})
				.GetJsonAsync<JObject>();
		}
	}
}
