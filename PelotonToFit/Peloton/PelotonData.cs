using Common;
using Common.Database;
using Peloton.Dto;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Peloton
{
	public class PelotonData
	{
		private Configuration _config;
		private ApiClient _pelotonApi;
		private DbClient _dbClient;

		public PelotonData(Configuration config, ApiClient pelotonApi, DbClient dbClient)
		{
			_config = config;
			_pelotonApi = pelotonApi;
			_dbClient = dbClient;
		}

		public async Task<ICollection<WorkoutData>> DownloadLatestWorkoutDataAsync() 
		{
			if (_config.Peloton.NumWorkoutsToDownload <= 0) return new List<WorkoutData>();

			using var tracing = Tracing.Trace(nameof(DownloadLatestWorkoutDataAsync));

			var items = new List<WorkoutData>();

			await _pelotonApi.InitAuthAsync();
			var recentWorkouts = await _pelotonApi.GetWorkoutsAsync(_config.Peloton.NumWorkoutsToDownload);
			var completedWorkouts = recentWorkouts.data.Where(w => w.Status == "COMPLETE");

			var outputDir = _config.App.PelotonDirectory;
			if (!Directory.Exists(outputDir))
			{
				Log.Debug("Creating directory {@Directory}", outputDir);
				Directory.CreateDirectory(outputDir);
			}

			foreach(var recentWorkout in recentWorkouts.data)
			{
				var workoutId = recentWorkout.Id;

				SyncHistoryItem syncRecord = _dbClient.Get(recentWorkout.Id);
				if ((syncRecord?.ConvertedToFit ?? false) && _config.Garmin.IgnoreSyncHistory == false)
				{
					Log.Information("Workout {0} already synced, skipping.", recentWorkout.Id);
					continue;
				}

				var workoutDir = Path.Join(outputDir, workoutId);
				if (!Directory.Exists(workoutDir))
				{
					Log.Debug("Creating directory {@Directory}", workoutDir);
					Directory.CreateDirectory(workoutDir);
				}

				var workoutTask = _pelotonApi.GetWorkoutByIdAsync(recentWorkout.Id);
				var workoutSamplesTask = _pelotonApi.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
				var workoutSummaryTask = _pelotonApi.GetWorkoutSummaryByIdAsync(recentWorkout.Id);

				await Task.WhenAll(workoutTask, workoutSamplesTask, workoutSummaryTask);

				var workout = workoutTask.GetAwaiter().GetResult();
				var workoutSamples = workoutSamplesTask.GetAwaiter().GetResult();
				var workoutSummary = workoutSummaryTask.GetAwaiter().GetResult();

				Log.Debug("Write peloton workout files.");
				File.WriteAllText(Path.Join(workoutDir, $"{workoutId}_workout.json"), JsonSerializer.Serialize(workout, new JsonSerializerOptions() { WriteIndented = true }));
				File.WriteAllText(Path.Join(workoutDir, $"{workoutId}_workoutSamples.json"), JsonSerializer.Serialize(workoutSamples, new JsonSerializerOptions() { WriteIndented = true }));
				File.WriteAllText(Path.Join(workoutDir, $"{workoutId}_workoutSummary.json"), JsonSerializer.Serialize(workoutSummary, new JsonSerializerOptions() { WriteIndented = true }));

				var startTimeInSeconds = workout.Start_Time;
				var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
				dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToLocalTime();

				var workoutData = new WorkoutData() 
				{
					Workout = workout,
					WorkoutSamples = workoutSamples,
					WorkoutSummary = workoutSummary,
					SyncHistoryItem = new SyncHistoryItem()
					{
						Id = workout.Id,
						WorkoutTitle = workout.Ride.Title,
						WorkoutDate = dtDateTime,
						DownloadDate = DateTime.Now,
					}
				};

				items.Add(workoutData);
			}

			return items;
		}
	}

	public class WorkoutData
	{
		public Workout Workout { get; set; }
		public WorkoutSamples WorkoutSamples { get; set; }
		public WorkoutSummary WorkoutSummary { get; set; }
		public SyncHistoryItem SyncHistoryItem { get; set; }
	}
}
