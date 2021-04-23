using Common;
using Common.Database;
using Common.Dto;
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

		public PelotonService(Configuration config, ApiClient pelotonApi, DbClient dbClient)
		{
			_config = config;
			_pelotonApi = pelotonApi;
			_dbClient = dbClient;
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
			FileHandling.MkDirIfNotEists(workingDir);

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

				if (_config.Format.Json && _config.Format.SaveLocalCopy) SaveRawData(data, workoutId);

				Log.Debug("Write peloton workout details to file for {@WorkoutId}.", workoutId);
				File.WriteAllText(Path.Join(workingDir, $"{workoutId}_workout.json"), data.ToString());

				P2GWorkout deSerializedData = JsonSerializer.Deserialize<P2GWorkout>(data.ToString(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

				var syncHistoryItem = new SyncHistoryItem(deSerializedData.Workout)
				{
					DownloadDate = DateTime.Now,
				};

				_dbClient.Upsert(syncHistoryItem);
			}
		}

		private void SaveRawData(dynamic data, string workoutId)
		{
			var outputDir = _config.App.JsonDirectory;
			FileHandling.MkDirIfNotEists(outputDir);

			Log.Debug("Write peloton json to file for {@WorkoutId}", workoutId);
			File.WriteAllText(Path.Join(outputDir, $"{workoutId}_workout.json"), data.ToString());
		}
	}
}
