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
		private readonly bool _observabilityEnabled;

		private string UserId;
		private string SessionId;

		public ApiClient(Settings config, AppConfiguration appConfig)
		{
			_userEmail = config.Peloton.Email;
			_userPassword = config.Peloton.Password;
			_observabilityEnabled = appConfig.Observability.Prometheus.Enabled;
		}

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
				.ConfigureRequest((c) =>
				{
					c.BeforeCallAsync = null;
					c.BeforeCallAsync = (FlurlCall call) =>
					{
						_logger.Verbose("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", call.HttpRequestMessage.Method, call.HttpRequestMessage.RequestUri, call.HttpRequestMessage.Headers.ToString(),"userAuthParams");
						return Task.CompletedTask;
					};
				})
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
			.ConfigureRequest((c) => 
			{
				c.AfterCallAsync = async (FlurlCall call) => 
				{
					_logger.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", 
								call.HttpResponseMessage?.StatusCode, 
								call.HttpRequestMessage?.Method, 
								call.HttpRequestMessage?.RequestUri, 
								call.HttpResponseMessage.Headers.ToString(), 
								await call.HttpResponseMessage?.Content.ReadAsStringAsync());

					if (_observabilityEnabled)
					{
						FlurlConfiguration.HttpRequestHistogram
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

		public Task<UserData> GetUserDataAsync()
		{
			return $"{BaseUrl}/me"
			.WithCookie("peloton_session_id", SessionId)
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
				.ConfigureRequest((c) =>
				{
					c.AfterCallAsync = async (FlurlCall call) =>
					{
						_logger.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
								call.HttpResponseMessage?.StatusCode,
								call.HttpRequestMessage?.Method,
								call.HttpRequestMessage?.RequestUri,
								call.HttpResponseMessage.Headers.ToString(),
								await call.HttpResponseMessage?.Content.ReadAsStringAsync());

						if (_observabilityEnabled)
						{
							FlurlConfiguration.HttpRequestHistogram
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
						_logger.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
								call.HttpResponseMessage?.StatusCode,
								call.HttpRequestMessage?.Method,
								call.HttpRequestMessage?.RequestUri,
								call.HttpResponseMessage.Headers.ToString(),
								await call.HttpResponseMessage?.Content.ReadAsStringAsync());

						if (_observabilityEnabled)
						{
							FlurlConfiguration.HttpRequestHistogram
							.WithLabels(
								call.HttpRequestMessage.Method.ToString(),
								call.HttpRequestMessage.RequestUri.Host,
								"/workout/{workoutid}/performance_graph",
								"?every_n={everyn}&joins=effort_zones",
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
