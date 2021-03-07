
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
		private static string[] ScriptPathParts = new string[] { Environment.CurrentDirectory, "..", "..", "..", "..", "..", "garminUpload.py" };
		private static string ScriptPath = Path.Combine(ScriptPathParts);

		public static bool UploadToGarmin(ICollection<string> filePaths, Configuration config)
		{
			var uploadScriptPath = config.Garmin.PathToGarminUploadPy ?? ScriptPath;
			var pythonPath = config.Application.PathToPythonExe;

			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = pythonPath;

			var paths = String.Join(" ", filePaths.Select(p => $"\"{p}\""));
			var cmd = $"{uploadScriptPath} -ge {config.Garmin.Email} -gp {config.Garmin.Password} -f {paths}";

			if (config.Application.DebugSeverity == Severity.Debug)
			{
				Console.WriteLine("Uploading to Garmin with the following parameters:");
				Console.WriteLine($"Python: {pythonPath}");
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

			var uploadScriptPath = config.Garmin.PathToGarminUploadPy ?? ScriptPath;
			if (!File.Exists(uploadScriptPath))
			{
				Console.Out.WriteLine($"File does not exist, check your configuration {nameof(config.Garmin)}.{nameof(config.Garmin.PathToGarminUploadPy)} is correct: {uploadScriptPath}");
				return false;
			}

			var pythonPath = config.Application.PathToPythonExe;
			if (!File.Exists(pythonPath))
			{
				Console.Out.WriteLine($"File does not exist, check your configuration {nameof(config.Application)}.{nameof(config.Application.PathToPythonExe)} is correct: {pythonPath}");
				return false;
			}

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
