using Common.Dto;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Garmin.Auth;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Metrics = Prometheus.Metrics;

namespace Garmin
{
	public interface IGarminUploader
	{
		Task UploadToGarminAsync();
	}

	public class GarminUploader : IGarminUploader
	{
		private static readonly Histogram WorkoutUploadDuration = Metrics.CreateHistogram($"{Statics.MetricPrefix}_workout_upload_duration_seconds", "Histogram of workout upload durations.", new HistogramConfiguration()
		{
			LabelNames = new[] { Common.Observe.Metrics.Label.Count }
		});
		private static readonly Gauge FailedUploadAttemptsGauge = Metrics.CreateGauge($"{Statics.MetricPrefix}_failed_upload_attempts",
			"The number of consecutive failed upload attempts. Resets to 0 on the first successful upload. This is not a count of the number of workouts that failed to upload. P2G uploads in bulk, so this is just a guage of number of failed upload attempts.");
		private static readonly Gauge FilesToUpload = Metrics.CreateGauge($"{Statics.MetricPrefix}_files_to_upload",
			"The number of files available to be uploaded. This number sets to 0 upon successful upload.");
		private static readonly ILogger _logger = LogContext.ForClass<GarminUploader>();

		private readonly ISettingsService _settingsService;
		private readonly IGarminApiClient _api;
		private readonly IGarminAuthenticationService _authService;
		private readonly Random _random;

		public GarminUploader(ISettingsService settingsService, IGarminApiClient api, IGarminAuthenticationService authService)
		{
			_settingsService = settingsService;
			_api = api;
			_random = new Random();
			_authService = authService;
		}

		public async Task UploadToGarminAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(GarminUploader)}.{nameof(UploadToGarminAsync)}");

			var settings = await _settingsService.GetSettingsAsync();

			if (!settings.Garmin.Upload) return;

			_logger.Information("Uploading workouts to Garmin...");

			if (!Directory.Exists(settings.App.UploadDirectory))
			{
				_logger.Information("No upload directory found. Done.");
				return;
			}

			var files = Directory.GetFiles(settings.App.UploadDirectory);
			tracing?.AddTag("workouts.count", files.Length);

			if (files.Length == 0)
			{
				_logger.Information("No files to upload in output directory. Done.");
				return;
			}

			using var metrics = WorkoutUploadDuration
								.WithLabels(files.Count().ToString()).NewTimer();

			await UploadAsync(files, settings);
			_logger.Information("Upload complete.");
		}

		private async Task UploadAsync(string[] files, Settings settings)
		{
			using var tracing = Tracing.Trace($"{nameof(GarminUploader)}.{nameof(UploadAsync)}.UploadToGarminViaNative")?
										.WithTag(TagKey.Category, "nativeImplV1")
										.AddTag("workouts.count", files.Count());

			var auth = await _authService.GetGarminAuthenticationAsync();

			if (auth.AuthStage == Dto.AuthStage.NeedMfaToken)
				throw new GarminUploadException("User needs to go through MFA flow to re-authenticate with Garmin. AuthStage: NeedMfaToken", -2);

			if (auth.AuthStage == Dto.AuthStage.None)
				throw new GarminUploadException("Expected user to be authenticated with Garmin at this point, but they are not. AuthStage: None.", -3);

			foreach (var file in files)
			{
				try
				{
					_logger.Information("Uploading to Garmin: {@file}", file);
					await _api.UploadActivity(file, settings.Format.Fit ? ".fit" : ".tcx", auth);
					await RateLimit();
				} catch (Exception e)
				{
					throw new GarminUploadException($"NativeImplV1 failed to upload workout {file}, {e.Message}", -1, e);
				}
			}
		}

		private async Task RateLimit()
		{
			using var tracing = Tracing.Trace($"{nameof(GarminUploader)}.{nameof(RateLimit)}");

			var waitDuration = _random.Next(1000, 5000);
			_logger.Information($"Rate limiting, upload will continue after {waitDuration / 1000} seconds...");
			tracing?.AddTag("rate.limit.sec", waitDuration);
			await Task.Delay(waitDuration);
		}

		public static void ValidateConfig(Settings config)
		{
			if (config.Garmin.Upload == false) return;

			config.Garmin.EnsureGarminCredentialsAreProvided();
		}
	}
}
