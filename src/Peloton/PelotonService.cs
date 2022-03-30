﻿using Common;
using Common.Database;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Observe;
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
	public interface IPelotonService
	{
		Task<ICollection<RecentWorkout>> GetRecentWorkoutsAsync(int numWorkoutsToDownload);
		Task<P2GWorkout[]> GetP2GWorkoutsAsync(ICollection<RecentWorkout> workoutIds);
		Task DownloadLatestWorkoutDataAsync();
		Task DownloadLatestWorkoutDataAsync(int numWorkoutsToDownload);
	}

	public class PelotonService : IPelotonService
	{
		public static readonly Histogram WorkoutDownloadDuration = Metrics.CreateHistogram("p2g_peloton_workout_download_duration_seconds", "Histogram of the entire time to download a single workouts data.");
		public static readonly Gauge FailedDesiralizationCount = Metrics.CreateGauge("p2g_peloton_workout_download_deserialization_failed", "Number of workouts that failed to deserialize during the last sync.");
		private static readonly ILogger _logger = LogContext.ForClass<PelotonService>();

		private Settings _config;
		private IPelotonApi _pelotonApi;
		private int _failedCount;
		private IFileHandling _fileHandler;

		public PelotonService(Settings config, IPelotonApi pelotonApi, IFileHandling fileHandler)
		{
			_config = config;
			_pelotonApi = pelotonApi;
			_fileHandler = fileHandler;

			_failedCount = 0;
		}

		public Task DownloadLatestWorkoutDataAsync()
		{
			return DownloadLatestWorkoutDataAsync(_config.Peloton.NumWorkoutsToDownload);
		}

		public async Task DownloadLatestWorkoutDataAsync(int numWorkoutsToDownload) 
		{
			if (numWorkoutsToDownload <= 0) return;

			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(DownloadLatestWorkoutDataAsync)}");

			await _pelotonApi.InitAuthAsync();

			var recentWorkouts = await GetRecentWorkoutsAsync(numWorkoutsToDownload);

			_logger.Debug("Total workouts found: {@FoundWorkouts}", recentWorkouts.Count);

			var completedWorkouts = recentWorkouts.Where(w => 
			{
				if (w.Status == "COMPLETE") return true;
				_logger.Debug("Skipping in progress workout. {@WorkoutId} {@WorkoutStatus} {@WorkoutType}", w.Id, w.Status, w.Fitness_Discipline);
				return false;
			});

			_logger.Debug("Total workouts found after filtering out InProgress: {@FoundWorkouts}", completedWorkouts.Count());

			var workingDir = _config.App.DownloadDirectory;
			_fileHandler.MkDirIfNotExists(workingDir);

			foreach (var recentWorkout in completedWorkouts)
			{
				var workoutId = recentWorkout.Id;

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
					_logger.Error("Failed to deserialize workout from Peloton. You can find the raw data from the workout here: @FileName", title, e);
					SaveRawData(data, title);
					continue;
				}

				_logger.Debug("Check if workout is in ExcludeWorkoutTypes list. {@WorkoutId}", workoutId);
				if (_config.Peloton.ExcludeWorkoutTypes.Contains(deSerializedData.WorkoutType))
				{
					_logger.Debug("Skipping excluded workout type. {@WorkoutId} {@WorkoutType}", deSerializedData.Workout.Id, deSerializedData.WorkoutType);
					continue;
				}

				_logger.Debug("Write peloton workout details to file for {@WorkoutId}.", workoutId);
				_fileHandler.WriteToFile(Path.Join(workingDir, $"{workoutTitle}.json"), data.ToString());
			}

			FailedDesiralizationCount.Set(_failedCount);
			if (_failedCount > 0)
				_logger.Warning("Failed to deserialize {@NumFailed} workouts. You can find the failed workouts at {@Path}", _failedCount, _config.App.JsonDirectory);
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

		public async Task<ICollection<RecentWorkout>> GetRecentWorkoutsAsync(int numWorkoutsToDownload)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetRecentWorkoutsAsync)}");

			await _pelotonApi.InitAuthAsync();

			List<RecentWorkout> recentWorkouts = new List<RecentWorkout>();
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

			return recentWorkouts;
		}

		private void SaveRawData(dynamic data, string workoutTitle)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(SaveRawData)}");

			var outputDir = _config.App.JsonDirectory;
			_fileHandler.MkDirIfNotExists(outputDir);

			_logger.Debug("Write peloton json to file for {@WorkoutId}", data.Workout.Id);
			File.WriteAllText(Path.Join(outputDir, $"{workoutTitle}.json"), data.ToString());
		}

		public Task<P2GWorkout[]> GetP2GWorkoutsAsync(ICollection<RecentWorkout> workoutIds)
		{
			var tasks = new List<Task<P2GWorkout>>();
			foreach (var recentWorkout in workoutIds)
			{
				var workoutId = recentWorkout.Id;

				tasks.Add(GetWorkoutDetailsAsync(workoutId));
			}

			return Task.WhenAll(tasks);
		}

		private async Task<P2GWorkout> GetWorkoutDetailsAsync(string workoutId)
		{
			using var workoutTimer = WorkoutDownloadDuration.NewTimer();

			var workoutTask = _pelotonApi.GetWorkoutByIdAsync(workoutId);
			var workoutSamplesTask = _pelotonApi.GetWorkoutSamplesByIdAsync(workoutId);

			await Task.WhenAll(workoutTask, workoutSamplesTask);

			var workout = await workoutTask;
			var workoutSamples = await workoutSamplesTask;

			return BuildP2GWorkout(workoutId, workout, workoutSamples);
		}

		private P2GWorkout BuildP2GWorkout(string workoutId, JObject workout, JObject workoutSamples)
		{
			dynamic data = new JObject();
			data.Workout = workout;
			data.WorkoutSamples = workoutSamples;

			P2GWorkout deSerializedData = null;
			try
			{
				deSerializedData = JsonSerializer.Deserialize<P2GWorkout>(data.ToString(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
				var workoutTitle = WorkoutHelper.GetUniqueTitle(deSerializedData.Workout);

				if (_config.Format.Json && _config.Format.SaveLocalCopy) SaveRawData(data, workoutTitle);

			}
			catch (Exception e)
			{
				_failedCount++;
				var title = "workout_failed_to_deserialize_" + workoutId;
				_logger.Error("Failed to deserialize workout from Peloton. You can find the raw data from the workout here: @FileName", title, e);
				SaveRawData(data, title);
			}

			return deSerializedData;
		}
	}
}
