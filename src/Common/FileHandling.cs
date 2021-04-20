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
				MkDirIfNotEists(toPath);
				toPath = Path.Join(toPath, Path.GetFileName(fromPath));
				Log.Debug("Moving failed file from {@FromPath} to {@ToPath}", fromPath, toPath);
				File.Copy(fromPath, toPath, overwrite: true);

			} catch (Exception e)
			{
				Log.Error(e, "Failed to move file from {@FromPath} to {@ToPath}", fromPath, toPath);
			}
		}

		public static void Cleanup(string dir)
		{
			if (!Directory.Exists(dir))
				return;

			try
			{
				Log.Debug("Deleting directory.");
				Directory.Delete(dir, recursive: true);
			} 
			catch (Exception e) {
				Log.Error(e, "Failed to clean up working directory: {@Directory}", dir);
			}
		}
	}
}
