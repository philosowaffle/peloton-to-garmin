﻿using Common;
using Common.Database;
using Common.Dto;
using Common.Helpers;
using Newtonsoft.Json.Linq;
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
	public class PelotonService
	{
		public static readonly Histogram WorkoutDownloadDuration = Metrics.CreateHistogram("p2g_peloton_workout_download_duration_seconds", "Histogram of the entire time to download a single workouts data.");
		public static readonly Gauge FailedDesiralizationCount = Metrics.CreateGauge("p2g_peloton_workout_download_deserialization_failed", "Number of workouts that failed to deserialize during the last sync.");

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

			await _pelotonApi.InitAuthAsync();

			List<RecentWorkout> recentWorkouts = new List<RecentWorkout>();
			var page = 0;
			var numWorkoutsToDownload = _config.Peloton.NumWorkoutsToDownload;
			while(numWorkoutsToDownload > 0)
			{
				Log.Debug("Fetching recent workouts page: {@Page}", page);

				var workouts = await _pelotonApi.GetWorkoutsAsync(_config.Peloton.NumWorkoutsToDownload, page);
				if (workouts.data is null || workouts.data.Count <= 0)
				{
					Log.Debug("No more workouts found from Peloton.");
					break;
				}

				recentWorkouts.AddRange(workouts.data);

				numWorkoutsToDownload -= workouts.data.Count;
				page++;
			}

			Log.Debug("Total workouts found: {@FoundWorkouts}", recentWorkouts.Count);

			var completedWorkouts = recentWorkouts.Where(w => 
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

			Log.Debug("Total workouts found after filtering InProgress and ExcludeWorkoutTypes: {@FoundWorkouts}", filteredWorkouts.Count());

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

				await Task.WhenAll(workoutTask, workoutSamplesTask);

				var workout = workoutTask.GetAwaiter().GetResult();
				var workoutSamples = workoutSamplesTask.GetAwaiter().GetResult();

				dynamic data = new JObject();
				data.Workout = workout;
				data.WorkoutSamples = workoutSamples;

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
					var title = "workout_failed_to_deserialize_" + workoutId;
					Log.Error("Failed to deserialize workout from Peloton. You can find the raw data from the workout here: @FileName", title, e);
					SaveRawData(data, title);
					continue;
				}				

				Log.Debug("Write peloton workout details to file for {@WorkoutId}.", workoutId);
				File.WriteAllText(Path.Join(workingDir, $"{workoutTitle}.json"), data.ToString());

				var syncHistoryItem = new SyncHistoryItem(deSerializedData.Workout)
				{
					DownloadDate = DateTime.Now,
				};

				_dbClient.Upsert(syncHistoryItem);
			}

			FailedDesiralizationCount.Set(_failedCount);
			if (_failedCount > 0)
				Log.Warning("Failed to deserialize {@NumFailed} workouts. You can find the failed workouts at {@Path}", _failedCount, _config.App.JsonDirectory);
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
