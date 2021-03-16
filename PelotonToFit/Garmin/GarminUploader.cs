using Common;
using Prometheus;
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
			using var tracer = Tracing.Source.StartActivity(nameof(UploadToGarmin))?
								.SetTag(Tracing.Category, Tracing.Http)?
								.SetTag(Tracing.App, "gupload");

			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = "gupload";

			var paths = String.Join(" ", filePaths.Select(p => $"\"{p}\""));
			var cmd = $"-u {config.Garmin.Email} -p {config.Garmin.Password} {paths}";

			if (config.Observability.LogLevel == Severity.Debug)
			{
				Console.WriteLine("Uploading to Garmin with the following parameters:");
				Console.WriteLine($"File Paths: {paths}");
				Console.WriteLine($"Full command: {cmd.Replace(config.Garmin.Email, "**email**").Replace(config.Garmin.Password, "**password**")}");
			}

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

					if (config.Observability.LogLevel == Severity.Debug)
						Console.Out.WriteLine(result);

					if (!string.IsNullOrEmpty(stderr))
					{
						Console.Out.WriteLine(stderr);
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
				Console.Out.WriteLine($"Garmin Email required, check your configuration {nameof(Garmin)}.{nameof(config.Email)} is set.");
				return false;
			}

			if (string.IsNullOrEmpty(config.Password))
			{
				Console.Out.WriteLine($"Garmin Password required, check your configuration {nameof(Garmin)}.{nameof(config.Password)} is set.");
				return false;
			}

			return true;
		}
	}
}
