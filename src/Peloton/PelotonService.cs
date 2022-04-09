﻿using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Observe;
using Common.Stateful;
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
		Task<P2GWorkout[]> GetWorkoutDetailsAsync(ICollection<RecentWorkout> workoutIds);
		Task DownloadLatestWorkoutDataAsync(int numWorkoutsToDownload);
	}

	public class PelotonService : IPelotonService
	{
		public static readonly Histogram WorkoutDownloadDuration = Metrics.CreateHistogram($"{Statics.MetricPrefix}_peloton_workout_download_duration_seconds", "Histogram of the entire time to download a single workouts data.");
		public static readonly Gauge FailedDesiralizationCount = Metrics.CreateGauge($"{Statics.MetricPrefix}_peloton_workout_download_deserialization_failed", "Number of workouts that failed to deserialize during the last sync.");
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

		public async Task DownloadLatestWorkoutDataAsync(int numWorkoutsToDownload) 
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(DownloadLatestWorkoutDataAsync)}")
										.WithTag("workouts.requested", numWorkoutsToDownload.ToString());

			if (numWorkoutsToDownload <= 0) return;

			await _pelotonApi.InitAuthAsync();

			var recentWorkouts = await GetRecentWorkoutsAsync(numWorkoutsToDownload);
			_logger.Debug("Total workouts found: {@FoundWorkouts}", recentWorkouts.Count);
			tracing.AddTag("workouts.found", recentWorkouts.Count);

			var completedWorkouts = recentWorkouts.Where(w => 
			{
				if (w.Status == "COMPLETE") return true;
				_logger.Debug("Skipping in progress workout. {@WorkoutId} {@WorkoutStatus} {@WorkoutType} {@WorkoutTitle}", w.Id, w.Status, w.Fitness_Discipline, w.Title);
				return false;
			});
			_logger.Debug("Total workouts found after filtering out InProgress: {@FoundWorkouts}", completedWorkouts.Count());
			tracing.AddTag("workouts.completed", completedWorkouts.Count());

			var workingDir = _config.App.DownloadDirectory;
			_fileHandler.MkDirIfNotExists(workingDir);

			foreach (var recentWorkout in completedWorkouts)
			{
				var workoutId = recentWorkout.Id;

				P2GWorkout deSerializedData = await GetWorkoutDetailsAsync(workoutId);
				var workoutTitle = WorkoutHelper.GetUniqueTitle(deSerializedData.Workout);

				if (_config.Format.Json && _config.Format.SaveLocalCopy) SaveRawData(deSerializedData.Raw, workoutTitle);

				_logger.Debug("Check if workout is in ExcludeWorkoutTypes list. {@WorkoutId}", workoutId);
				if (_config.Peloton.ExcludeWorkoutTypes.Contains(deSerializedData.WorkoutType))
				{
					_logger.Debug("Skipping excluded workout type. {@WorkoutId} {@WorkoutType}", deSerializedData.Workout.Id, deSerializedData.WorkoutType);
					continue;
				}

				_logger.Debug("Write peloton workout details to file for {@WorkoutId}.", workoutId);
				_fileHandler.WriteToFile(Path.Join(workingDir, $"{workoutTitle}.json"), deSerializedData.Raw.ToString());
			}

			FailedDesiralizationCount.Set(_failedCount);
			if (_failedCount > 0)
			{
				_logger.Warning("Failed to deserialize {@NumFailed} workouts. You can find the failed workouts at {@Path}", _failedCount, _config.App.JsonDirectory);
				tracing.AddTag("workouts.failed", _failedCount);
			}
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
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetRecentWorkoutsAsync)}")
										.WithTag("workouts.requested", numWorkoutsToDownload.ToString());

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
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(SaveRawData)}")
										.WithTag("workout.title", workoutTitle);

			var outputDir = _config.App.JsonDirectory;
			_fileHandler.MkDirIfNotExists(outputDir);

			_logger.Debug("Write peloton json to file for {@WorkoutId}", data.Workout.Id);
			_fileHandler.WriteToFile(Path.Join(outputDir, $"{workoutTitle}.json"), data.ToString());
		}

		public async Task<P2GWorkout[]> GetWorkoutDetailsAsync(ICollection<RecentWorkout> workoutIds)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetWorkoutDetailsAsync)}.List");

			await _pelotonApi.InitAuthAsync();

			var tasks = new List<Task<P2GWorkout>>();
			foreach (var recentWorkout in workoutIds)
			{
				var workoutId = recentWorkout.Id;

				tasks.Add(GetWorkoutDetailsAsync(workoutId));
			}

			return await Task.WhenAll(tasks);
		}

		private async Task<P2GWorkout> GetWorkoutDetailsAsync(string workoutId)
		{
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(GetWorkoutDetailsAsync)}.Item")
										.WithWorkoutId(workoutId);

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
			using var tracing = Tracing.Trace($"{nameof(PelotonService)}.{nameof(BuildP2GWorkout)}")
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
				_logger.Error("Failed to deserialize workout from Peloton. You can find the raw data from the workout here: @FileName", title, e);
				tracing.AddTag("exception.message", e.Message);
				SaveRawData(data, title);
			}

			return deSerializedData;
		}
	}
}
