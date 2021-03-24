using Serilog;
using System;
using System.IO;

namespace Common
{
	public static class FileHandling
	{
		public static void MkDirIfNotEists(string path)
		{
			if (!Directory.Exists(path))
			{
				Log.Debug("Creating directory {@Directory}", path);
				Directory.CreateDirectory(path);
			}
		}

		public static void MoveFailedFile(string fromPath, string toPath)
		{
			try
			{
				Log.Debug("Moving failed file from {@FromPath} to {@ToPath}", fromPath, toPath);
				MkDirIfNotEists(toPath);
				File.Copy(fromPath, toPath, overwrite: true);

			} catch (Exception e)
			{
				Log.Error(e, "Failed to move file from {@FromPath} to {@ToPath}", fromPath, toPath);
			}
		}

		public static void Cleanup(string workingDir)
		{
			try
			{
				Log.Debug("Deleting working directory.");
				Directory.Delete(workingDir, recursive: true);
			} 
			catch (Exception e) {
				Log.Error(e, "Failed to clean up working directory: {@Directory}", workingDir);
			}
		}
	}
}
