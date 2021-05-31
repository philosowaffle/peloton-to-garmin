using Serilog;
using System;
using System.IO;
using System.Text.Json;

namespace Common
{
	public interface IFileHandling
	{
		void MkDirIfNotExists(string path);
		bool DirExists(string path);
		string[] GetFiles(string path);

		T DeserializeJson<T>(string file);
		void MoveFailedFile(string fromPath, string toPath);
		void Copy(string from, string to, bool overwrite);
		void Cleanup(string dir);
	}

	public class IOWrapper : IFileHandling
	{
		public void MkDirIfNotExists(string path)
		{
			using var trace1 = Tracing.Trace(nameof(MkDirIfNotExists), "io");
			if (!DirExists(path))
			{
				using var trace2 = Tracing.Trace("CreateDirectory", "io");
				Log.Debug("Creating directory {@Directory}", path);
				Directory.CreateDirectory(path);
			}
		}

		public bool DirExists(string path)
		{
			using var trace1 = Tracing.Trace(nameof(DirExists), "io");
			return Directory.Exists(path);
		}

		public string[] GetFiles(string path)
		{
			using var trace1 = Tracing.Trace(nameof(GetFiles), "io");
			return Directory.GetFiles(path);
		}

		public T DeserializeJson<T>(string file)
		{
			using var trace1 = Tracing.Trace(nameof(DeserializeJson), "io");
			using (var reader = new StreamReader(file))
			{
				return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
			}
		}

		public void MoveFailedFile(string fromPath, string toPath)
		{
			using var trace1 = Tracing.Trace(nameof(MoveFailedFile), "io");
			try
			{
				MkDirIfNotExists(toPath);
				toPath = Path.Join(toPath, Path.GetFileName(fromPath));
				Log.Debug("Moving failed file from {@FromPath} to {@ToPath}", fromPath, toPath);
				File.Copy(fromPath, toPath, overwrite: true);

			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to move file from {@FromPath} to {@ToPath}", fromPath, toPath);
			}
		}

		public void Copy(string from, string to, bool overwrite)
		{
			using var trace1 = Tracing.Trace(nameof(Copy), "io");
			File.Copy(from, to, overwrite);
		}

		public void Cleanup(string dir)
		{
			using var trace1 = Tracing.Trace(nameof(Cleanup), "io");
			if (!DirExists(dir))
				return;

			try
			{
				using var trace2 = Tracing.Trace("DeleteDir", "io");
				Log.Debug("Deleting directory.");
				Directory.Delete(dir, recursive: true);
			}
			catch (Exception e)
			{
				Log.Error(e, "Failed to clean up working directory: {@Directory}", dir);
			}
		}
	}
}
