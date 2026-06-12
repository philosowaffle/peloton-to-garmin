using Common.Dto.Peloton;
using Common.Stateful;
using Flurl.Http;
using Peloton.AnnualChallenge;
using Peloton.Auth;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Peloton
{
	public interface IPelotonApi
	{
		Task<PagedPelotonResponse<Workout>> GetWorkoutsAsync(int pageSize, int page);
		Task<PelotonResponse<Workout>> GetWorkoutsAsync(DateTime fromUtc, DateTime toUtc);
		Task<Workout> GetWorkoutByIdAsync(string id);
		Task<WorkoutSamples> GetWorkoutSamplesByIdAsync(string id);
		Task<UserData> GetUserDataAsync();
		Task<PelotonChallenges> GetJoinedChallengesAsync(int userId);
		Task<PelotonUserChallengeDetail> GetUserChallengeDetailsAsync(int userId, string challengeId);
		Task<RideSegments> GetClassSegmentsAsync(string rideId);
	}

	public class ApiClient : IPelotonApi
	{
		private static readonly string BaseUrl = "https://api.onepeloton.com/api";
		private readonly IPelotonAuthApiClient _authApiClient;

		public ApiClient(IPelotonAuthApiClient authApiClient)
		{
			_authApiClient = authApiClient;
		}

		public Task<PelotonApiAuthentication> GetAuthAsync(string overrideUserAgent = null)
		{
			return _authApiClient.Authenticate();
		}

		public async Task<PagedPelotonResponse<Workout>> GetWorkoutsAsync(int pageSize, int page)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{auth.UserId}/workouts"
			.WithOAuthBearerToken(auth.Token.AccessToken)
			.WithCommonHeaders()
			.SetQueryParams(new
			{
				limit = pageSize,
				sort_by = "-created",
				page = page,
				joins= "ride,ride.instructor"
			})
			.GetJsonAsync<PagedPelotonResponse<Workout>>();
		}

		public async Task<PelotonResponse<Workout>> GetWorkoutsAsync(DateTime fromUtc, DateTime toUtc)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{auth.UserId}/workouts"
			.WithOAuthBearerToken(auth.Token.AccessToken)
			.WithCommonHeaders()
			.SetQueryParams(new
			{
				from = fromUtc.ToString("o", CultureInfo.InvariantCulture),
				to = toUtc.ToString("o", CultureInfo.InvariantCulture),
				sort_by = "-created",
				joins = "ride"
			})
			.GetJsonAsync<PelotonResponse<Workout>>();
		}

		/// <summary>
		/// For ad hoc testing and contract discovery.
		/// </summary>
		public async Task<string> GetRawWorkoutsAsync(string userId, int numWorkouts, int page)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{userId}/workouts"
			.WithOAuthBearerToken(auth.Token.AccessToken)
			.WithCommonHeaders()
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created",
				page = page,
				joins = "ride"
			})
			.GetStringAsync();
		}

		/// <summary>
		/// For ad hoc testing and contract discovery.
		/// </summary>
		public async Task<string> GetRawWorkoutByIdAsync(string id)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/workout/{id}"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.SetQueryParams(new
				{
					joins = "ride,ride.instructor"
				})
				.GetStringAsync();
		}

		/// <summary>
		/// For ad hoc testing and contract discovery.
		/// </summary>
		public async Task<string> GetRawWorkoutSamplesByIdAsync(string id)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/workout/{id}/performance_graph"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.SetQueryParams(new
				{
					every_n = 1
				})
				.GetStringAsync();
		}

		/// <summary>
		/// For ad hoc testing and contract discovery.
		/// </summary>
		public async Task<string> GetRawClassSegmentsAsync(string rideId)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/ride/{rideId}/details"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.GetStringAsync();
		}

		public async Task<UserData> GetUserDataAsync()
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/me"
			.WithOAuthBearerToken(auth.Token.AccessToken)
			.WithCommonHeaders()
			.GetJsonAsync<UserData>();
		}

		public async Task<Workout> GetWorkoutByIdAsync(string id)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/workout/{id}"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.SetQueryParams(new
				{
					joins = "ride,ride.instructor"
				})
				.GetJsonAsync<Workout>();
		}

		public async Task<WorkoutSamples> GetWorkoutSamplesByIdAsync(string id)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/workout/{id}/performance_graph"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.SetQueryParams(new
				{
					every_n=1
				})
				.GetJsonAsync<WorkoutSamples>();
		}

		public async Task<PelotonChallenges> GetJoinedChallengesAsync(int userId)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{auth.UserId}/challenges/current"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.SetQueryParams(new
				{
					has_joined = true
				})
				.GetJsonAsync<PelotonChallenges>();
		}

		public async Task<PelotonUserChallengeDetail> GetUserChallengeDetailsAsync(int userId, string challengeId)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/user/{auth.UserId}/challenge/{challengeId}"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.GetJsonAsync<PelotonUserChallengeDetail>();
		}

		public async Task<RideSegments> GetClassSegmentsAsync(string rideId)
		{
			var auth = await GetAuthAsync();
			return await $"{BaseUrl}/ride/{rideId}/details"
				.WithOAuthBearerToken(auth.Token.AccessToken)
				.WithCommonHeaders()
				.GetJsonAsync<RideSegments>();
		}
	}
}