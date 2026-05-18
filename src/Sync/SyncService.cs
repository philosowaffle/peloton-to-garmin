using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Conversion;
using Garmin;
using Garmin.Auth;
using Peloton;
using Prometheus;
using Serilog;
using Strava;
using Sync.Database;
using Sync.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sync
{
	public interface ISyncService
	{
		Task<SyncResult> SyncAsync(int numWorkouts, bool forceStackClasses);
		Task<SyncResult> SyncAsync(IEnumerable<string> workoutIds, ICollection<WorkoutType>? exclude = null, bool forceStackWorkouts = false);
	}

	public class SyncService : ISyncService
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncService>();
		private static readonly Histogram SyncHistogram = Prometheus.Metrics.CreateHistogram($"{Statics.MetricPrefix}_sync_duration_seconds", "The histogram of sync jobs that have run.");

		private readonly IStravaService _stravaService;
		private readonly IGarminUploader _garminUploader;
		private readonly IEnumerable<IConverter> _converters;
		private readonly ISyncStatusDb _db;
		private readonly IFileHandling _fileHandler;
		private readonly ISettingsService _settingsService;

		public SyncService(ISettingsService settingService, IStravaService stravaService, IGarminUploader garminUploader, IEnumerable<IConverter> converters, ISyncStatusDb dbClient, IFileHandling fileHandler)
		{
			_settingsService = settingService;
			_stravaService = stravaService;
			_garminUploader = garminUploader;
			_converters = converters;
			_db = dbClient;
			_fileHandler = fileHandler;
		}

		public async Task<SyncResult> SyncAsync(int numWorkouts, bool forceStackClasses = false)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}.ByNumWorkouts")
										.WithTag("numWorkouts", numWorkouts.ToString());

			var settings = await _settingsService.GetSettingsAsync();
			return await SyncWithWorkoutLoaderAsync(() => LoadStravaActivitiesAsWorkoutsAsync(numWorkouts), settings.Strava.ExcludeActivityTypes, forceStackClasses);
		}

		public async Task<SyncResult> SyncAsync(IEnumerable<string> workoutIds, ICollection<WorkoutType>? exclude = null, bool forceStackClasses = false)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}.ByWorkoutIds");

			var response = new SyncResult();
			var recentWorkouts = workoutIds.Select(w => new Workout() { Id = w }).ToList();
			var settings = await _settingsService.GetSettingsAsync();

			UserData? userData = null;
			try
			{
				userData = await _stravaService.GetAthleteDataAsync();
			}
			catch (Exception e)
			{
				_logger.Warning(e, $"Failed to fetch athlete data from Strava: {e.Message}, FTP info may be missing for certain non-class workout types (Just Ride).");
			}

			P2GWorkout[] workouts = { };
			try
			{
				workouts = await LoadStravaActivitiesAsWorkoutsDetailedAsync(recentWorkouts);
				response.StravaDownloadSuccess = true;
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Failed to download activities from Strava.");
				response.SyncSuccess = false;
				response.StravaDownloadSuccess = false;
				response.Errors.Add(new ServiceError() { Message = $"Failed to download activities from Strava. {e.Message} - Check logs for more details." });
				return response;
			}

			var filteredWorkouts = workouts.Where(w => 
								{
									if (w is null) return false;

									if (exclude is null || exclude.Count == 0) return true;

									if (exclude.Contains(w.WorkoutType))
									{
										_logger.Debug("Skipping excluded workout type. {@WorkoutId} {@WorkoutType}", w.Workout.Id, w.WorkoutType);
										return false;
									}

									return true;
								});

			var filteredWorkoutsCount = filteredWorkouts.Count();
			activity?.AddTag("workouts.filtered", filteredWorkoutsCount);
			_logger.Information("Found {@NumWorkouts} workouts remaining after filtering ExcludedWorkoutTypes.", filteredWorkoutsCount);

			if (!filteredWorkouts.Any())
			{
				_logger.Information("No workouts to sync. Sync complete.");
				response.ConversionSuccess = true;
				response.SyncSuccess = true;
				return response;
			}

			// calculate stacked workouts
			var stackedWorkouts = filteredWorkouts;
			if (settings.Format.StackedWorkouts.AutomaticallyStackWorkouts || forceStackClasses)
			{
				_logger.Debug("Stacking classes.");
				var stackedClassesMaxAllowedGapSeconds = forceStackClasses ? long.MaxValue : settings.Format.StackedWorkouts.MaxAllowedGapSeconds;
				var stacks = StackedWorkoutsCalculator.GetStackedWorkouts(filteredWorkouts, stackedClassesMaxAllowedGapSeconds);
				stackedWorkouts = StackedWorkoutsCalculator.CombineStackedWorkouts(stacks);
				_logger.Debug($"{filteredWorkoutsCount} workouts yielded {stacks.Count()} stacks.");
			}

			var convertStatuses = new List<ConvertStatus>();
			try
			{
				_logger.Information("Converting workouts...");
				var tasks = new List<Task<ConvertStatus>>();
				foreach (var workout in stackedWorkouts)
				{
					workout.UserData = userData;
					tasks.AddRange(_converters.Select(c => c.ConvertAsync(workout)));
				}

				await Task.WhenAll(tasks);
				convertStatuses = tasks.Select(t => t.GetAwaiter().GetResult()).ToList();
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Unexpected error. Failed to convert workouts. {e.Message}");

				response.SyncSuccess = false;
				response.ConversionSuccess = false;
				response.Errors.Add(new ServiceError() { Message = $"Unexpected error. Failed to convert workouts. {e.Message} Check logs for more details." });
				return response;
			}

			if (!convertStatuses.Any() || convertStatuses.All(c => c.Result == ConversionResult.Skipped))
			{
				_logger.Information("All converters were skipped. Ensure you have atleast one output Format configured in your settings. Converting to FIT or TCX is required prior to uploading to Garmin Connect.");
				response.SyncSuccess = false;
				response.ConversionSuccess = false;
				response.Errors.Add(new ServiceError() { Message = "All converters were skipped. Ensure you have atleast one output Format configured in your settings. Converting to FIT or TCX is required prior to uploading to Garmin Connect." });
				return response;
			}

			if (convertStatuses.All(c => c.Result == ConversionResult.Failed))
			{
				_logger.Error("All configured converters failed to convert workouts.");
				response.SyncSuccess = false;
				response.ConversionSuccess = false;
				response.Errors.Add(new ServiceError() { Message = "All configured converters failed to convert workouts. Successfully, converting to FIT or TCX is required prior to uploading to Garmin Connect. See logs for more details." });
				return response;
			}

			foreach (var convertStatus in convertStatuses)
				if (convertStatus.Result == ConversionResult.Failed)
					response.Errors.Add(new ServiceError() { Message = convertStatus.ErrorMessage });

			response.ConversionSuccess = true;

			try
			{
				await _garminUploader.UploadToGarminAsync();
				response.UploadToGarminSuccess = true;
			}
			catch (ArgumentException ae)
			{
				_logger.Error(ae, $"Sync failed to upload to Garmin Connect. {ae.Message}");

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ServiceError() { Message = $"Failed to upload workouts to Garmin Connect. {ae.Message}", Exception = ae });
				return response;
			}
			catch (GarminAuthenticationError gae)
			{
				_logger.Error(gae, $"Garmin Uploader failed to authenticate with Garmin. {gae.Message}");

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ServiceError() { Message = gae.Message, Exception = gae });
				return response;
			}
			catch (GarminUploadException gue)
			{
				_logger.Error(gue, $"Garmin Uploader failed to upload to Garmin Connect. {gue.Message}");

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ServiceError() { Message = gue.Message, Exception = gue });
				return response;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Unexpected error. Failed to upload workouts to Garmin Connect. You can find the converted files at {@Path} \\n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", settings.App.OutputDirectory);

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ServiceError() { Message = $"Failed to upload workouts to Garmin Connect. {e.Message}", Exception = e });
				return response;
			}
			finally
			{
				_fileHandler.Cleanup(settings.App.DownloadDirectory);
				_fileHandler.Cleanup(settings.App.UploadDirectory);
				_fileHandler.Cleanup(settings.App.WorkingDirectory);
			}

			response.SyncSuccess = true;
			return response;
		}

		private IEnumerable<string> FilterToCompletedWorkoutIds(ICollection<Workout> workouts)
		{
			return workouts?
					.Where(w =>
					{
						var shouldKeep = w.Status == "COMPLETE";
						if (shouldKeep) return true;

						_logger.Debug("Skipping in progress workout. {@WorkoutId} {@WorkoutStatus} {@WorkoutType} {@WorkoutTitle}", w.Id, w.Status, w.Fitness_Discipline, w.Title);
						return false;
					})
					.Select(r => r.Id) ?? new List<string>();
		}

		private async Task<SyncResult> SyncWithWorkoutLoaderAsync(Func<Task<ServiceResult<ICollection<Workout>>>> loader, ICollection<WorkoutType>? exclude, bool forceStackClasses = false)
		{
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}.SyncWithWorkoutLoaderAsync");

			ICollection<Workout> recentWorkouts;
			var syncTime = await _db.GetSyncStatusAsync();
			var settings = await _settingsService.GetSettingsAsync();
			syncTime.LastSyncTime = DateTime.Now;

			try
			{
				var recentWorkoutsServiceResult = await loader();
				recentWorkouts = recentWorkoutsServiceResult.Result;
			}
			catch (ArgumentException ae)
			{
				var errorMessage = $"Failed to fetch activities from Strava: {ae.Message}";

				_logger.Error(ae, errorMessage);
				activity?.AddTag("exception.message", ae.Message);
				activity?.AddTag("exception.stacktrace", ae.StackTrace);

				syncTime.SyncStatus = Status.UnHealthy;
				syncTime.LastErrorMessage = errorMessage;
				await _db.UpsertSyncStatusAsync(syncTime);

				var response = new SyncResult();
				response.SyncSuccess = false;
				response.StravaDownloadSuccess = false;
				response.Errors.Add(new ServiceError() { Message = $"{errorMessage}" });
				return response;
			}
			catch (Exception ex)
			{
				var errorMessage = "Failed to fetch activities from Strava.";

				_logger.Error(ex, errorMessage);
				activity?.AddTag("exception.message", ex.Message);
				activity?.AddTag("exception.stacktrace", ex.StackTrace);

				syncTime.SyncStatus = Status.UnHealthy;
				syncTime.LastErrorMessage = errorMessage;
				await _db.UpsertSyncStatusAsync(syncTime);

				var response = new SyncResult();
				response.SyncSuccess = false;
				response.StravaDownloadSuccess = false;
				response.Errors.Add(new ServiceError() { Message = $"{errorMessage} Check logs for more details." });
				return response;
			}

			var completedWorkouts = FilterToCompletedWorkoutIds(recentWorkouts);

			var completedWorkoutsCount = completedWorkouts.Count();
			_logger.Information("Found {@NumWorkouts} completed workouts.", completedWorkoutsCount);
			activity?.AddTag("workouts.completed", completedWorkoutsCount);

			var result = await SyncAsync(completedWorkouts, settings.Strava.ExcludeActivityTypes, forceStackClasses);

			if (result.SyncSuccess)
				syncTime.LastSuccessfulSyncTime = DateTime.Now;

			await _db.UpsertSyncStatusAsync(syncTime);

			return result;
		}
	}
}
private async Task<ServiceResult<ICollection<Workout>>> LoadStravaActivitiesAsWorkoutsAsync(int numActivities)
{
var result = new ServiceResult<ICollection<Workout>>();
var activities = await _stravaService.GetRecentActivitiesAsync(numActivities);

// Конвертируем активности Strava в формат Workout
var workouts = new List<Workout>();
foreach (var activity in activities)
{
var workout = ConvertStravaActivityToWorkout(activity);
workouts.Add(workout);
}

result.Result = workouts;
return result;
}

private async Task<ServiceResult<ICollection<P2GWorkout>>> LoadStravaActivitiesAsWorkoutsDetailedAsync(IEnumerable<Workout> workouts)
{
var result = new ServiceResult<ICollection<P2GWorkout>>();

var p2gWorkouts = new List<P2GWorkout>();
foreach (var workout in workouts)
{
if (string.IsNullOrEmpty(workout.Id))
continue;

if (long.TryParse(workout.Id, out var activityId))
{
var activityWithStreams = await _stravaService.GetActivityDetailsAsync(activityId);
var p2gWorkout = ConvertStravaActivityWithStreamsToP2GWorkout(activityWithStreams);
p2gWorkouts.Add(p2gWorkout);
}
}

result.Result = p2gWorkouts;
return result;
}

private Workout ConvertStravaActivityToWorkout(StravaActivity activity)
{
return new Workout
{
Id = activity.Id.ToString(),
Title = activity.Name,
Status = "COMPLETE", // Strava активности всегда завершены
Fitness_Discipline = MapStravaTypeToWorkoutType(activity.SportType),
Start_Time = activity.StartDateLocal,
End_Time = activity.StartDateLocal.AddSeconds(activity.ElapsedTime),
Duration = activity.MovingTime,
Distance = activity.Distance
};
}

private P2GWorkout ConvertStravaActivityWithStreamsToP2GWorkout(StravaActivityWithStreams activity)
{
// Здесь будет полная конвертация с потоками данных
// Для GPX нам нужны координаты, время, пульс, мощность и т.д.

var workout = new P2GWorkout
{
Workout = new Workout
{
Id = activity.Id.ToString(),
Title = activity.Name,
Status = "COMPLETE",
Fitness_Discipline = MapStravaTypeToWorkoutType(activity.SportType),
Start_Time = activity.StartDateLocal,
End_Time = activity.StartDateLocal.AddSeconds(activity.ElapsedTime),
Duration = activity.MovingTime,
Distance = activity.Distance
},
Raw = activity // Сохраняем оригинальные данные Strava
};

return workout;
}

private WorkoutType MapStravaTypeToWorkoutType(string stravaSportType)
{
return stravaSportType?.ToLower() switch
{
"run" or "trail_run" => WorkoutType.Run,
"ride" or "virtual_ride" or "mountain_bike_ride" or "gravel_ride" => WorkoutType.Cycling,
"walk" or "hike" => WorkoutType.Walk,
"weight_training" or "workout" or "crossfit" => WorkoutType.StrengthTraining,
"swim" => WorkoutType.Swimming,
"row" or "virtual_row" => WorkoutType.Rowing,
_ => WorkoutType.None
};
}
}
}
