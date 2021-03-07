
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Garmin
{
	public static class GarminUploader
	{
		private static string[] ScriptPathParts = new string[] { Environment.CurrentDirectory, "..", "..", "..", "..", "..", "garminUpload.py" };
		private static string ScriptPath = Path.Combine(ScriptPathParts);

		public static bool UploadToGarmin(ICollection<string> filePaths, string garminEmail, string garminPassword, string pathToPythonExe)
		{
			ProcessStartInfo start = new ProcessStartInfo();
			start.FileName = pathToPythonExe;

			var paths = String.Join(" -f ", filePaths);
			var cmd = $"{ScriptPath} -ge {garminEmail} -gp {garminPassword} -f {paths}";
			
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
	}
}
