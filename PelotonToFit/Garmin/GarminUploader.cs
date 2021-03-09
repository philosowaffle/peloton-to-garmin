
using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Garmin
{
	public static class GarminUploader
	{

		public static bool UploadToGarmin(ICollection<string> filePaths, Configuration config)
		{
			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = "gupload";

			var paths = String.Join(" ", filePaths.Select(p => $"\"{p}\""));
			var cmd = $"-u {config.Garmin.Email} -p {config.Garmin.Password} {paths}";

			if (config.Application.DebugSeverity == Severity.Debug)
			{
				Console.WriteLine("Uploading to Garmin with the following parameters:");
				Console.WriteLine($"Python: {start.FileName}");
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

					if (config.Application.DebugSeverity == Severity.Debug)
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

		public static bool ValidateConfig(Configuration config)
		{
			if (config.Garmin.Upload == false) return true;

			if (string.IsNullOrEmpty(config.Garmin.Email))
			{
				Console.Out.WriteLine($"Garmin Email required, check your configuration {nameof(config.Garmin)}.{nameof(config.Garmin.Email)} is set.");
				return false;
			}

			if (string.IsNullOrEmpty(config.Garmin.Password))
			{
				Console.Out.WriteLine($"Garmin Password required, check your configuration {nameof(config.Garmin)}.{nameof(config.Garmin.Password)} is set.");
				return false;
			}

			return true;
		}
	}
}
