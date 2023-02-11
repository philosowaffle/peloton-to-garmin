using Common;
using Common.Database;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sync
{
	public interface ISyncService
	{
		Task<SyncResult> SyncAsync(int numWorkouts);
		Task<SyncResult> SyncAsync(IEnumerable<string> workoutIds, ICollection<WorkoutType>? exclude = null);
	}

	public class SyncService : ISyncService
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncService>();
		private static readonly Histogram SyncHistogram = Prometheus.Metrics.CreateHistogram($"{Statics.MetricPrefix}_sync_duration_seconds", "The histogram of sync jobs that have run.");

		private readonly IPelotonService _pelotonService;
		private readonly IGarminUploader _garminUploader;
		private readonly IEnumerable<IConverter> _converters;
		private readonly ISyncStatusDb _db;
		private readonly IFileHandling _fileHandler;
		private readonly ISettingsService _settingsService;

		public SyncService(ISettingsService settingService, IPelotonService pelotonService, IGarminUploader garminUploader, IEnumerable<IConverter> converters, ISyncStatusDb dbClient, IFileHandling fileHandler)
		{
			_settingsService = settingService;
			_pelotonService = pelotonService;
			_garminUploader = garminUploader;
			_converters = converters;
			_db = dbClient;
			_fileHandler = fileHandler;
		}

		public async Task<SyncResult> SyncAsync(int numWorkouts)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}.ByNumWorkouts")
										.WithTag("numWorkouts", numWorkouts.ToString());

			var settings = await _settingsService.GetSettingsAsync();
			return await SyncWithWorkoutLoaderAsync(() => _pelotonService.GetRecentWorkoutsAsync(numWorkouts), settings.Peloton.ExcludeWorkoutTypes);
		}

		public async Task<SyncResult> SyncAsync(IEnumerable<string> workoutIds, ICollection<WorkoutType>? exclude = null)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}.ByWorkoutIds");

			var response = new SyncResult();
			var recentWorkouts = workoutIds.Select(w => new Workout() { Id = w }).ToList();
			var settings = await _settingsService.GetSettingsAsync();

			UserData? userData = null;
			try
			{
				userData = await _pelotonService.GetUserDataAsync();
			}
			catch (Exception e)
			{
				_logger.Warning(e, $"Failed to fetch user data from Peloton: {e.Message}, FTP info may be missing for certain non-class workout types (Just Ride).");
			}

			P2GWorkout[] workouts = { };
			try
			{
				workouts = await _pelotonService.GetWorkoutDetailsAsync(recentWorkouts);
				response.PelotonDownloadSuccess = true;
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Failed to download workouts from Peloton.");
				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"Failed to download workouts from Peloton. {e.Message} - Check logs for more details." });
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

			var convertStatuses = new List<ConvertStatus>();
			try
			{
				_logger.Information("Converting workouts...");
				var tasks = new List<Task<ConvertStatus>>();
				foreach (var workout in filteredWorkouts)
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
				response.Errors.Add(new ErrorResponse() { Message = $"Unexpected error. Failed to convert workouts. {e.Message} Check logs for more details." });
				return response;
			}

			if (!convertStatuses.Any() || convertStatuses.All(c => c.Result == ConversionResult.Skipped))
			{
				_logger.Information("All converters were skipped. Ensure you have atleast one output Format configured in your settings. Converting to FIT or TCX is required prior to uploading to Garmin Connect.");
				response.SyncSuccess = false;
				response.ConversionSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "All converters were skipped. Ensure you have atleast one output Format configured in your settings. Converting to FIT or TCX is required prior to uploading to Garmin Connect." });
				return response;
			}

			if (convertStatuses.All(c => c.Result == ConversionResult.Failed))
			{
				_logger.Error("All configured converters failed to convert workouts.");
				response.SyncSuccess = false;
				response.ConversionSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "All configured converters failed to convert workouts. Successfully, converting to FIT or TCX is required prior to uploading to Garmin Connect. See logs for more details." });
				return response;
			}

			foreach (var convertStatus in convertStatuses)
				if (convertStatus.Result == ConversionResult.Failed)
					response.Errors.Add(new ErrorResponse() { Message = convertStatus.ErrorMessage });

			response.ConversionSuccess = true;

			try
			{
				await _garminUploader.UploadToGarminAsync();
				response.UploadToGarminSuccess = true;
			}
			catch (ArgumentException ae)
			{
				_logger.Error(ae, $"Failed to upload to Garmin Connect. {ae.Message}");

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"Failed to upload workouts to Garmin Connect. {ae.Message}" });
				return response;
			}
			catch (GarminAuthenticationError gae)
			{
				_logger.Error(gae, $"Sync failed to authenticate with Garmin. {gae.Message}");

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = gae.Message });
				return response;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to upload workouts to Garmin Connect. You can find the converted files at {@Path} \\n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", settings.App.OutputDirectory);

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"Failed to upload workouts to Garmin Connect. {e.Message}" });
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
			return workouts
					.Where(w =>
					{
						var shouldKeep = w.Status == "COMPLETE";
						if (shouldKeep) return true;

						_logger.Debug("Skipping in progress workout. {@WorkoutId} {@WorkoutStatus} {@WorkoutType} {@WorkoutTitle}", w.Id, w.Status, w.Fitness_Discipline, w.Title);
						return false;
					})
					.Select(r => r.Id);
		}

		private async Task<SyncResult> SyncWithWorkoutLoaderAsync(Func<Task<ServiceResult<ICollection<Workout>>>> loader, ICollection<WorkoutType>? exclude)
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
				var errorMessage = $"Failed to fetch workouts from Peloton: {ae.Message}";

				_logger.Error(ae, errorMessage);
				activity?.AddTag("exception.message", ae.Message);
				activity?.AddTag("exception.stacktrace", ae.StackTrace);

				syncTime.SyncStatus = Status.UnHealthy;
				syncTime.LastErrorMessage = errorMessage;
				await _db.UpsertSyncStatusAsync(syncTime);

				var response = new SyncResult();
				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"{errorMessage}" });
				return response;
			}
			catch (Exception ex)
			{
				var errorMessage = "Failed to fetch workouts from Peloton.";

				_logger.Error(ex, errorMessage);
				activity?.AddTag("exception.message", ex.Message);
				activity?.AddTag("exception.stacktrace", ex.StackTrace);

				syncTime.SyncStatus = Status.UnHealthy;
				syncTime.LastErrorMessage = errorMessage;
				await _db.UpsertSyncStatusAsync(syncTime);

				var response = new SyncResult();
				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"{errorMessage} Check logs for more details." });
				return response;
			}

			var completedWorkouts = FilterToCompletedWorkoutIds(recentWorkouts);

			var completedWorkoutsCount = completedWorkouts.Count();
			_logger.Information("Found {@NumWorkouts} completed workouts.", completedWorkoutsCount);
			activity?.AddTag("workouts.completed", completedWorkoutsCount);

			var result = await SyncAsync(completedWorkouts, settings.Peloton.ExcludeWorkoutTypes);

			if (result.SyncSuccess)
				syncTime.LastSuccessfulSyncTime = DateTime.Now;

			await _db.UpsertSyncStatusAsync(syncTime);

			return result;
		}
	}
}