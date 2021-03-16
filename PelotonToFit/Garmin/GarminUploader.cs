using Common;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Metrics = Common.Metrics;

namespace Garmin
{
	public static class GarminUploader
	{
		public static bool UploadToGarmin(ICollection<string> filePaths, Configuration config)
		{
			if (!config.Garmin.Upload || !filePaths.Any())
				return true;

			using var metrics = Metrics.WorkoutUploadDuration
								.WithLabels(filePaths.Count.ToString()).NewTimer();
			using var tracer = Tracing.Trace(nameof(UploadToGarmin), TagValue.Http)
										.SetTag(TagKey.App, "gupload");

			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = "gupload";

			var paths = String.Join(" ", filePaths.Select(p => $"\"{p}\""));
			var cmd = $"-u {config.Garmin.Email} -p {config.Garmin.Password} {paths}";

			Log.Debug("Uploading to Garmin with the following parameters: {0} {1}", paths, cmd.Replace(config.Garmin.Email, "**email**").Replace(config.Garmin.Password, "**password**"));

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

					Log.Debug(result);

					if (!string.IsNullOrEmpty(stderr))
					{
						Log.Error(stderr);
						return false;
					}
				}
			}

			return true;
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
