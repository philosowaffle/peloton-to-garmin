using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Peloton.Dto;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Metrics = Prometheus.Metrics;

namespace Peloton
{
	public interface IPelotonService
	{
		/// <summary>
		/// Fetches N number of recent workouts.
		/// </summary>
		/// <param name="numWorkoutsToDownload"></param>
		/// <returns></returns>
		Task<ServiceResult<ICollection<Workout>>> GetRecentWorkoutsAsync(int numWorkoutsToDownload);
		Task<ServiceResult<ICollection<Workout>>> GetWorkoutsSinceAsync(DateTime sinceDt);
		/// <summary>
		/// Fetches workouts by Page.
		/// </summary>
		/// <param name="pageSize"></param>
		/// <param name="pageIndex"></param>
		/// <returns></returns>
		Task<PagedPelotonResponse<Workout>> GetPelotonWorkoutsAsync(int pageSize, int pageIndex);
		Task<P2GWorkout[]> GetWorkoutDetailsAsync(ICollection<Workout> workoutIds);
		Task<UserData> GetUserDataAsync();
	}

	public class PelotonService : IPelotonService
	{
		public static readonly Histogram WorkoutDownloadDuration = Metrics.CreateHistogram($"{Statics.MetricPrefix}_peloton_workout_download_duration_seconds", "Histogram of the entire time to download a single workouts data.");
		public static readonly Gauge FailedDesiralizationCount = Metrics.CreateGauge($"{Statics.MetricPrefix}_peloton_workout_download_deserialization_failed", "Number of workouts that failed to deserialize during the last sync.");
		private static readonly ILogger _logger = LogContext.ForClass<PelotonService>();

		private readonly ISettingsService _settingsService;
		private readonly IPelotonApi _pelotonApi;
		private readonly IFileHandling _fileHandler;

		private int _failedCount;

		public PelotonService(ISettingsService settingsService, IPelotonApi pelotonApi, IFileHandling fileHandler)
		{
			_settingsService = settingsService;
			_pelotonApi = pelotonApi;
			_fileHandler = fileHandler;

			_failedCount = 0;
		}

		public static void ValidateConfig(Common.Peloton config)
		{
			if (string.IsNullOrEmpty(config.Email))
			{
				_logger.Error("Peloton Email required, check your configuration {@ConfigSection}.{@ConfigProperty} is set.", nameof(Peloton), nameof(config.Email));
				throw new ArgumentException("Peloton Email must be set.", nameof(config.Email));
			}

			if (string.IsNullOrEmpty(config.Password))
			{
				_logger.Error("Peloton Password required, check your configuration {@ConfigSection}.{@ConfigProperty} is set.", nameof(Peloton), nameof(config.Password));
				throw new ArgumentException("Peloton Password must be set.", nameof(config.Password));
			}
		}

		public async Task<UserData> GetUserDataAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetUserDataAsync)}");

			return await _pelotonApi.GetUserDataAsync();
		}

		public async Task<PagedPelotonResponse<Workout>> GetPelotonWorkoutsAsync(int pageSize, int pageIndex)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetPelotonWorkoutsAsync)}")
										.WithTag("workouts.pageSize", pageSize.ToString())
										.WithTag("workouts.pageIndex", pageIndex.ToString());

			var recentWorkouts = new PagedPelotonResponse<Workout>();

			if (pageSize <= 0 || pageIndex < 0) return recentWorkouts;

			recentWorkouts = await _pelotonApi.GetWorkoutsAsync(pageSize, pageIndex);

			_logger.Debug("Total workouts found: {@FoundWorkouts}", recentWorkouts.Count);
			tracing?.AddTag("workouts.found", recentWorkouts.Count);
			tracing?.AddTag("workouts.total", recentWorkouts.Total);

			return recentWorkouts;
		}

		public async Task<ServiceResult<ICollection<Workout>>> GetRecentWorkoutsAsync(int numWorkoutsToDownload)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetRecentWorkoutsAsync)}")
										.WithTag("workouts.requested", numWorkoutsToDownload.ToString());

			var result = new ServiceResult<ICollection<Workout>>();
			List<Workout> recentWorkouts = new List<Workout>();

			if (numWorkoutsToDownload <= 0) return result;
			
			try
			{
				var page = 0;
				while (numWorkoutsToDownload > 0)
				{
					_logger.Debug("Fetching recent workouts page: {@Page}", page);

					var workouts = await _pelotonApi.GetWorkoutsAsync(numWorkoutsToDownload, page);
					if (workouts.data is null || workouts.data.Count <= 0)
					{
						_logger.Debug("No more workouts found from Peloton.");
						break;
					}

					recentWorkouts.AddRange(workouts.data);

					numWorkoutsToDownload -= workouts.data.Count;
					page++;
				}

				_logger.Debug("Total workouts found: {@FoundWorkouts}", recentWorkouts.Count);
				tracing?.AddTag("workouts.found", recentWorkouts.Count);

				result.Result = recentWorkouts;
				return result;
			}
			catch (FlurlHttpTimeoutException fte)
			{
				result.Successful = false;
				result.Error = new ServiceError()
				{
					Exception = fte,
					IsServerException = true,
					Message = "Timed out trying to communicate with the Peloton API"
				};
				return result;
			}
			catch (PelotonAuthenticationError pe)
			{
				result.Successful = false;
				result.Error = new ServiceError()
				{
					Exception = pe,
					IsServerException = false,
					Message = pe.Message
				};
				return result;
			}
			catch (ArgumentException ae)
			{
				result.Successful = false;
				result.Error = new ServiceError()
				{
					Exception = ae,
					IsServerException = false,
					Message = ae.Message
				};
				return result;
			}
		}

		public async Task<ServiceResult<ICollection<Workout>>> GetWorkoutsSinceAsync(DateTime sinceDt)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetWorkoutsSinceAsync)}")
										.WithTag("workouts.sinceDt", sinceDt.ToString());
			
			var result = new ServiceResult<ICollection<Workout>>();

			try
			{
				var workouts = await _pelotonApi.GetWorkoutsAsync(fromUtc: sinceDt, toUtc: DateTime.UtcNow);
				result.Result = workouts.data;
				return result;
			}
			catch (FlurlHttpTimeoutException fte)
			{
				result.Successful = false;
				result.Error = new ServiceError()
				{
					Exception = fte,
					IsServerException = true,
					Message = "Timed out trying to communicate with the Peloton API"
				};
				return result;
			}
			catch (PelotonAuthenticationError pe)
			{
				result.Successful = false;
				result.Error = new ServiceError()
				{
					Exception = pe,
					IsServerException = false,
					Message = pe.Message
				};
				return result;
			}
			catch (ArgumentException ae)
			{
				result.Successful = false;
				result.Error = new ServiceError()
				{
					Exception = ae,
					IsServerException = false,
					Message = ae.Message
				};
				return result;
			}
		}

		public async Task<P2GWorkout[]> GetWorkoutDetailsAsync(ICollection<Workout> workoutIds)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetWorkoutDetailsAsync)}.List");

			if (workoutIds is null || workoutIds.Count() <= 0) return new P2GWorkout[0];

			var maxBatchSize = 25;
			var tasks = new List<Task<P2GWorkout>>(maxBatchSize);
			var results = new List<P2GWorkout>(workoutIds.Count);
			var stack = new Stack<Workout>(workoutIds);
			var batchSize = 0;
			while (stack.TryPop(out var popped))
			{
				batchSize++;
				tasks.Add(GetWorkoutDetailsAsync(popped.Id));

				if (batchSize >= maxBatchSize)
				{
					_logger.Verbose($"Fetching Batch Size: {batchSize}");
					var awaited = await Task.WhenAll(tasks);
					var successful = awaited.Where(t => t is object);
					results.AddRange(successful);

					batchSize = 0;
					tasks.Clear();
				}
			}

			if (tasks.Any())
			{
				var awaited = await Task.WhenAll(tasks);
				var successful = awaited.Where(t => t is object);
				results.AddRange(successful);
			}

			return results.ToArray();
		}

		public async Task<P2GWorkout> GetWorkoutDetailsAsync(string workoutId)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetWorkoutDetailsAsync)}.Item")
										.WithWorkoutId(workoutId);

			using var workoutTimer = WorkoutDownloadDuration.NewTimer();

			var workoutTask = _pelotonApi.GetWorkoutByIdAsync(workoutId);
			var workoutSamplesTask = _pelotonApi.GetWorkoutSamplesByIdAsync(workoutId);

			await Task.WhenAll(workoutTask, workoutSamplesTask);

			var workout = await workoutTask;
			var workoutSamples = await workoutSamplesTask;

			return await BuildP2GWorkoutAsync(workoutId, workout, workoutSamples);
		}

		private async Task<P2GWorkout> BuildP2GWorkoutAsync(string workoutId, JObject workout, JObject workoutSamples)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(BuildP2GWorkoutAsync)}")
										.WithWorkoutId(workoutId);

			dynamic data = new JObject();
			data.Workout = workout;
			data.WorkoutSamples = workoutSamples;

			P2GWorkout deSerializedData = null;
			try
			{
				deSerializedData = JsonSerializer.Deserialize<P2GWorkout>(data.ToString(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
				deSerializedData.Raw = data;
			}
			catch (Exception e)
			{
				_failedCount++;

				var title = "workout_failed_to_deserialize_" + workoutId;
				await SaveRawDataAsync(data, title);

				_logger.Error("Failed to deserialize workout from Peloton. You can find the raw data from the workout here: {@FileName}", title, e);
			}

			return deSerializedData;
		}

		private async Task SaveRawDataAsync(dynamic data, string workoutTitle)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(SaveRawDataAsync)}")
										.WithTag("workout.title", workoutTitle);

			var settings = await _settingsService.GetSettingsAsync();
			var outputDir = settings.App.FailedDirectory;
			_fileHandler.MkDirIfNotExists(outputDir);

			_logger.Debug("Write peloton json to file for {@WorkoutId}", data.Workout.Id);
			_fileHandler.WriteToFile(Path.Join(outputDir, $"{workoutTitle}.json"), data.ToString());
		}
	}
}
