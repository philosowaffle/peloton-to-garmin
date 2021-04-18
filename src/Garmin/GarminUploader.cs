using Common;
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
	public class GarminUploader
	{
		private static readonly Histogram WorkoutUploadDuration = Metrics.CreateHistogram("p2g_workout_upload_duration_seconds", "Histogram of workout upload durations.", new HistogramConfiguration()
		{
			LabelNames = new[] { "count" }
		});

		private readonly Configuration _config;
		private readonly ApiClient _api;

		public GarminUploader(Configuration config)
		{
			_config = config;
			_api = new ApiClient(config);
		}

		public void UploadToGarmin()
		{
			if (!_config.Garmin.Upload) return;

			if (!Directory.Exists(_config.App.UploadDirectory))
			{
				Log.Information("No upload directory found. Nothing to do.");
				return;
			}

			var files = Directory.GetFiles(_config.App.UploadDirectory);

			if (files.Length == 0)
			{
				Log.Information("No files to upload in output directory. Nothing to do.");
				return;
			}

			using var metrics = WorkoutUploadDuration
								.WithLabels(files.Count().ToString()).NewTimer();

			UploadViaPython(files);
			//Task.Run(() => Upload(files)).GetAwaiter().GetResult();

		}

		private async Task Upload(string[] files)
		{
			await _api.InitAuth();
			
			foreach (var file in files)
			{
				await _api.UploadActivity("someName", file, _config.Format.Fit ? ".fit" : ".tcx");
			}
		}

		private void UploadViaPython(string[] files)
		{
			using var tracer = Tracing.Trace(nameof(UploadToGarmin))
										.WithTag(TagKey.App, "gupload");
			
			ProcessStartInfo start = new ProcessStartInfo();
			var paths = String.Join(" ", files.Select(p => $"\"{p}\""));
			var cmd = string.Empty;

			if (_config.App.PythonAndGUploadInstalled)
			{
				start.FileName = "gupload";
				cmd = $"-u {_config.Garmin.Email} -p {_config.Garmin.Password} {paths}";
			} else
			{
				paths = String.Join(" ", files.Select(f => $"\"{Path.GetFullPath(f)}\""));
				start.FileName = Path.Join(Environment.CurrentDirectory, "python", "upload", "upload.exe");
				cmd = $"-ge {_config.Garmin.Email} -gp {_config.Garmin.Password} -f {paths}";
			}

			Log.Debug("Uploading to Garmin with the following parameters: {0} {1}", start.FileName, cmd.Replace(_config.Garmin.Email, "**email**").Replace(_config.Garmin.Password, "**password**"));

			start.Arguments = cmd;
			start.UseShellExecute = false;
			start.CreateNoWindow = true;
			start.RedirectStandardOutput = true;
			start.RedirectStandardError = true;
			using (Process process = Process.Start(start))
			{
				using (StreamReader reader = process.StandardOutput)
				{
					string stderr = process.StandardError.ReadToEnd();
					string result = reader.ReadToEnd();

					if (!string.IsNullOrEmpty(result))
						Log.Debug(result);

					// Despite coming from StandardError, this is not necessarily an error, just the output
					if (!string.IsNullOrEmpty(stderr))
						Log.Information("GUpload: {Output}", stderr);

					if (process.ExitCode != 0)
					{
						throw new GarminUploadException("Failed to upload workouts.", process.ExitCode);
					}
				}
			}
		}

		public static bool ValidateConfig(Common.Garmin config)
		{
			if (config.Upload == false) return true;

			if (string.IsNullOrEmpty(config.Email))
			{
				Log.Error("Garmin Email required, check your configuration {0}.{1} is set.", nameof(Garmin), nameof(config.Email));
				return false;
			}

			if (string.IsNullOrEmpty(config.Password))
			{
				Log.Error("Garmin Password required, check your configuration {0}.{1} is set.", nameof(Garmin), nameof(config.Password));
				return false;
			}

			return true;
		}
	}
}
