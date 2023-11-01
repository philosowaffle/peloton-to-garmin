using Common;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Garmin.Auth;
using Prometheus;
using Serilog;
using System;
using System.Diagnostics;
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

			switch (settings.Garmin.UploadStrategy)
			{
				case UploadStrategy.PythonAndGuploadInstalledLocally:
				case UploadStrategy.WindowsExeBundledPython:
					UploadViaPython(files, settings);
					_logger.Information("Upload complete.");
					return;
				case UploadStrategy.NativeImplV1:
				default:
					await UploadAsync(files, settings);
					_logger.Information("Upload complete.");
					return;
			}
		}

		private async Task UploadAsync(string[] files, Settings settings)
		{
			using var tracing = Tracing.Trace($"{nameof(GarminUploader)}.{nameof(UploadAsync)}.UploadToGarminViaNative")?
										.WithTag(TagKey.Category, "nativeImplV1")
										.AddTag("workouts.count", files.Count());

			var auth = await _authService.GetGarminAuthenticationAsync();

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

		private void UploadViaPython(string[] files, Settings settings)
		{
			using var tracing = Tracing.Trace($"{nameof(GarminUploader)}.{nameof(UploadViaPython)}.UploadToGarminViaPython")
										.WithTag(TagKey.Category, "gupload");

			settings.Garmin.EnsureGarminCredentialsAreProvided();

			ProcessStartInfo start = new ProcessStartInfo();
			var paths = String.Join(" ", files.Select(p => $"\"{p}\""));
			var cmd = string.Empty;

			if (settings.Garmin.UploadStrategy == UploadStrategy.PythonAndGuploadInstalledLocally)
			{
				start.FileName = "gupload";
				cmd = $"-u {settings.Garmin.Email} -p {settings.Garmin.Password} {paths}";
			} else
			{
				paths = String.Join(" ", files.Select(f => $"\"{Path.GetFullPath(f)}\""));
				start.FileName = Path.Join(Environment.CurrentDirectory, "python", "upload.exe");
				cmd = $"-ge {settings.Garmin.Email} -gp {settings.Garmin.Password} -f {paths}";
			}

			_logger.Information("Beginning Garmin Upload.");
			_logger.Information("Uploading to Garmin with the following parameters: {@File} {@Command}", start.FileName, cmd.Replace(settings.Garmin.Email, "**email**").Replace(settings.Garmin.Password, "**password**"));

			start.Arguments = cmd;
			start.UseShellExecute = false;
			start.CreateNoWindow = true;
			start.RedirectStandardOutput = true;
			start.RedirectStandardError = true;

			FilesToUpload.Set(files.Length);
			if (files.Length > 20)
				_logger.Information("Detected large number of files for upload to Garmin. Please be patient, this could take a while.");
			using var process = Process.Start(start);
			process.WaitForExit();

			var stderr = process.StandardError.ReadToEnd();
			var stdout = process.StandardOutput.ReadToEnd();

			if (!string.IsNullOrEmpty(stdout))
				_logger.Information(stdout);

			// Despite coming from StandardError, this is not necessarily an error, just the output
			if (!string.IsNullOrEmpty(stderr))
				_logger.Information("GUpload: {Output}", stderr);

			if (process.HasExited && process.ExitCode != 0)
			{
				FailedUploadAttemptsGauge.Inc();
				throw new GarminUploadException("GUpload returned an error code. Failed to upload workouts.", process.ExitCode);
			} else
			{
				FailedUploadAttemptsGauge.Set(0);
				FilesToUpload.Set(0);
			}
		}

		public static void ValidateConfig(Settings config)
		{
			if (config.Garmin.Upload == false) return;

			config.Garmin.EnsureGarminCredentialsAreProvided();

			if (config.App.EnablePolling && config.Garmin.TwoStepVerificationEnabled)
				throw new ArgumentException("App.EnablePolling cannot be true when Garmin.TwoStepVerificationEnabled is true.");

			if (config.App.PythonAndGUploadInstalled.HasValue)
			{
				_logger.Warning("App.PythonAndGuploadInstalledLocally setting is deprecated and will be removed in a future release. Please swith to using Garmin.UploadStrategy config.");

				if (config.Garmin.UploadStrategy == UploadStrategy.PythonAndGuploadInstalledLocally
					&& config.App.PythonAndGUploadInstalled.Value == false)
				{
					config.Garmin.UploadStrategy = UploadStrategy.WindowsExeBundledPython;
					_logger.Warning("Detected use of deprecated config App.PythonAndGuploadInstalledLocally, setting Garmin.UploadStrategy to WindowsExeBundledPython=1");
				}
			}
		}
	}
}
