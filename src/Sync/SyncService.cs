using Common;
using Common.Database;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Stateful;
using Conversion;
using Garmin;
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
		Task<SyncResult> SyncAsync(ICollection<string> workoutIds, ICollection<WorkoutType>? exclude = null);
	}

	public class SyncService : ISyncService
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncService>();
		private static readonly Histogram SyncHistogram = Prometheus.Metrics.CreateHistogram($"{Statics.MetricPrefix}_sync_duration_seconds", "The histogram of sync jobs that have run.");

		private readonly Settings _config;
		private readonly IPelotonService _pelotonService;
		private readonly IGarminUploader _garminUploader;
		private readonly IEnumerable<IConverter> _converters;
		private readonly ISyncStatusDb _db;
		private readonly IFileHandling _fileHandler;

		public SyncService(Settings config, IPelotonService pelotonService, IGarminUploader garminUploader, IEnumerable<IConverter> converters, ISyncStatusDb dbClient, IFileHandling fileHandler)
		{
			_config = config;
			_pelotonService = pelotonService;
			_garminUploader = garminUploader;
			_converters = converters;
			_db = dbClient;
			_fileHandler = fileHandler;
		}

		public async Task<SyncResult> SyncAsync(int numWorkouts)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}")
										.WithTag("numWorkouts", numWorkouts.ToString());

			ICollection<RecentWorkout> recentWorkouts;
			var syncTime = await _db.GetSyncStatusAsync();
			syncTime.LastSyncTime = DateTime.Now;

			try
			{
				recentWorkouts = await _pelotonService.GetRecentWorkoutsAsync(numWorkouts);
			}
			catch (ArgumentException ae)
			{
				var errorMessage = $"Failed to fetch recent workouts from Peleoton: {ae.Message}";

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
				var errorMessage = "Failed to fetch recent workouts from Peleoton.";

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

			var completedWorkouts = recentWorkouts
									.Where(w =>
									{
										var shouldKeep = w.Status == "COMPLETE";
										if (shouldKeep) return true;

										_logger.Debug("Skipping in progress workout. {@WorkoutId} {@WorkoutStatus} {@WorkoutType} {@WorkoutTitle}", w.Id, w.Status, w.Fitness_Discipline, w.Title);
										return false;
									})
									.Select(r => r.Id)
									.ToList();

			_logger.Debug("Total workouts found after filtering out InProgress: {@FoundWorkouts}", completedWorkouts.Count());
			activity?.AddTag("workouts.completed", completedWorkouts.Count());

			var result = await SyncAsync(completedWorkouts, _config.Peloton.ExcludeWorkoutTypes);

			if (result.SyncSuccess)
				syncTime.LastSuccessfulSyncTime = DateTime.Now;

			await _db.UpsertSyncStatusAsync(syncTime);

			return result;
		}

		public async Task<SyncResult> SyncAsync(ICollection<string> workoutIds, ICollection<WorkoutType>? exclude = null)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace($"{nameof(SyncService)}.{nameof(SyncAsync)}.ByWorkoutIds");

			var response = new SyncResult();
			var recentWorkouts = workoutIds.Select(w => new RecentWorkout() { Id = w }).ToList();

			UserData? userData = null;
			try
			{
				userData = await _pelotonService.GetUserDataAsync();

			}
			catch (ArgumentException ae)
			{
				var errorMessage = $"Failed to fetch recent workouts from Peleoton: {ae.Message}";

				_logger.Error(ae, errorMessage);
				activity?.AddTag("exception.message", ae.Message);
				activity?.AddTag("exception.stacktrace", ae.StackTrace);

				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"{errorMessage}" });
				return response;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to fetch UserData from Peloton. FTP info may be missing for certain non-class workout types (Just Ride).");
			}

			P2GWorkout[] workouts = { };
			try
			{
				workouts = await _pelotonService.GetWorkoutDetailsAsync(recentWorkouts);
				response.PelotonDownloadSuccess = true;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to download workouts from Peloton.");
				response.SyncSuccess = false;
				response.PelotonDownloadSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to download workouts from Peloton. Check logs for more details." });
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

			activity?.AddTag("workouts.filtered", filteredWorkouts.Count());
			_logger.Debug("Number of workouts to convert after filtering InProgress: {@NumWorkouts}", filteredWorkouts.Count());

			try
			{
				Parallel.ForEach(filteredWorkouts, (workout) => 
				{
					Parallel.ForEach(_converters, (converter) =>
					{
						workout.UserData = userData;
						converter.Convert(workout);
					});
				});

				response.ConversionSuccess = true;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to convert workouts to FIT format.");

				response.SyncSuccess = false;
				response.ConversionSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to convert workouts to FIT format. Check logs for more details." });
				return response;
			}

			try
			{
				await _garminUploader.UploadToGarminAsync();
				response.UploadToGarminSuccess = true;
			}
			catch (ArgumentException ae)
			{
				var errorMessage = $"Failed to upload to Garmin Connect: {ae.Message}";

				_logger.Error(ae, errorMessage);
				activity?.AddTag("exception.message", ae.Message);
				activity?.AddTag("exception.stacktrace", ae.StackTrace);

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = $"{errorMessage}" });
				return response;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to upload workouts to Garmin Connect. You can find the converted files at {@Path} \\n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", _config.App.OutputDirectory);

				response.SyncSuccess = false;
				response.UploadToGarminSuccess = false;
				response.Errors.Add(new ErrorResponse() { Message = "Failed to upload workouts to Garmin Connect. Check logs for more details." });
				return response;
			} finally
			{
				_fileHandler.Cleanup(_config.App.DownloadDirectory);
				_fileHandler.Cleanup(_config.App.UploadDirectory);
				_fileHandler.Cleanup(_config.App.WorkingDirectory);
			}

			response.SyncSuccess = true;
			return response;
		}
	}
}