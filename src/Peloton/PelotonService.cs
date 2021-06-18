using Common;
using Common.Database;
using Common.Dto;
using Common.Helpers;
using Newtonsoft.Json.Linq;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Metrics = Prometheus.Metrics;

namespace Peloton
{
	public class PelotonService
	{
		public static readonly Histogram WorkoutDownloadDuration = Metrics.CreateHistogram("p2g_peloton_workout_download_duration_seconds", "Histogram of the entire time to download a single workouts data.");

		private Configuration _config;
		private ApiClient _pelotonApi;
		private DbClient _dbClient;
		private int _failedCount;
		private IFileHandling _fileHandler;

		public PelotonService(Configuration config, ApiClient pelotonApi, DbClient dbClient, IFileHandling fileHandler)
		{
			_config = config;
			_pelotonApi = pelotonApi;
			_dbClient = dbClient;
			_fileHandler = fileHandler;

			_failedCount = 0;
		}

		public async Task DownloadLatestWorkoutDataAsync() 
		{
			if (_config.Peloton.NumWorkoutsToDownload <= 0) return;

			using var tracing = Tracing.Trace(nameof(DownloadLatestWorkoutDataAsync));

			await _pelotonApi.InitAuthAsync(_config.Developer?.UserAgent);

			var recentWorkouts = await _pelotonApi.GetWorkoutsAsync(_config.Peloton.NumWorkoutsToDownload);
			var completedWorkouts = recentWorkouts.data.Where(w => 
			{
				if (w.Status == "COMPLETE") return true;
				Log.Debug("Skipping in progress workout. {@WorkoutId} {@WorkoutStatus} {@WorkoutType}", w.Id, w.Status, w.Fitness_Discipline);
				return false;
			});

			var filteredWorkouts = completedWorkouts.Where(w => 
			{
				if (!_config.Peloton.ExcludeWorkoutTypes?.Contains(w.Fitness_Discipline) ?? true) return true;
				Log.Debug("Skipping excluded workout type. {@WorkoutId} {@WorkoutStatus} {@WorkoutType}", w.Id, w.Status, w.Fitness_Discipline);
				return false;
			});

			var workingDir = _config.App.DownloadDirectory;
			_fileHandler.MkDirIfNotExists(workingDir);

			foreach (var recentWorkout in filteredWorkouts)
			{
				var workoutId = recentWorkout.Id;

				SyncHistoryItem syncRecord = _dbClient.Get(recentWorkout.Id);
				if ((syncRecord?.DownloadDate is object))
				{
					Log.Debug("Workout {@WorkoutId} already downloaded, skipping.", recentWorkout.Id);
					continue;
				}

				using var workoutTimer = WorkoutDownloadDuration.NewTimer();

				var workoutTask = _pelotonApi.GetWorkoutByIdAsync(recentWorkout.Id);
				var workoutSamplesTask = _pelotonApi.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
				var workoutSummaryTask = _pelotonApi.GetWorkoutSummaryByIdAsync(recentWorkout.Id);

				await Task.WhenAll(workoutTask, workoutSamplesTask, workoutSummaryTask);

				var workout = workoutTask.GetAwaiter().GetResult();
				var workoutSamples = workoutSamplesTask.GetAwaiter().GetResult();
				var workoutSummary = workoutSummaryTask.GetAwaiter().GetResult();

				dynamic data = new JObject();
				data.Workout = workout;
				data.WorkoutSamples = workoutSamples;
				data.WorkoutSummary = workoutSummary;

				var workoutTitle = string.Empty;
				P2GWorkout deSerializedData = null;
				try
				{
					deSerializedData = JsonSerializer.Deserialize<P2GWorkout>(data.ToString(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
					workoutTitle = WorkoutHelper.GetUniqueTitle(deSerializedData.Workout);

					if (_config.Format.Json && _config.Format.SaveLocalCopy) SaveRawData(data, workoutTitle);

				} catch (Exception e)
				{
					_failedCount++;
					var title = "workout_failed_to_deserialize_" + _failedCount;
					Log.Error("Failed to deserialize workout from Peloton. You can find the raw data from the workout here: @FileName", title, e);
					SaveRawData(data, title);
					return;
				}				

				Log.Debug("Write peloton workout details to file for {@WorkoutId}.", workoutId);
				File.WriteAllText(Path.Join(workingDir, $"{workoutTitle}.json"), data.ToString());

				
				var syncHistoryItem = new SyncHistoryItem(deSerializedData.Workout)
				{
					DownloadDate = DateTime.Now,
				};

				_dbClient.Upsert(syncHistoryItem);
			}
		}

		public static void ValidateConfig(Common.Peloton config)
		{
			if (string.IsNullOrEmpty(config.Email))
			{
				Log.Error("Peloton Email required, check your configuration {@ConfigSection}.{@ConfigProperty} is set.", nameof(Peloton), nameof(config.Email));
				throw new ArgumentException("Peloton Email must be set.", nameof(config.Email));
			}

			if (string.IsNullOrEmpty(config.Password))
			{
				Log.Error("Peloton Password required, check your configuration {@ConfigSection}.{@ConfigProperty} is set.", nameof(Peloton), nameof(config.Password));
				throw new ArgumentException("Peloton Password must be set.", nameof(config.Password));
			}
		}

		private void SaveRawData(dynamic data, string workoutTitle)
		{
			var outputDir = _config.App.JsonDirectory;
			_fileHandler.MkDirIfNotExists(outputDir);

			Log.Debug("Write peloton json to file for {@WorkoutId}", data.Workout.Id);
			File.WriteAllText(Path.Join(outputDir, $"{workoutTitle}.json"), data.ToString());
		}
	}
}
