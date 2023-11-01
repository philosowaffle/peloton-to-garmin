using Common.Observe;
using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;

namespace Common
{
	public interface IFileHandling
	{
		void MkDirIfNotExists(string path);
		bool DirExists(string path);
		bool FileExists(string path);
		string[] GetFiles(string path);

		T DeserializeJson<T>(string file);
		bool TryDeserializeXml<T>(string file, out T result) where T : new();
		void MoveFailedFile(string fromPath, string toPath);
		void Copy(string from, string to, bool overwrite);
		bool WriteToFile(string path, string content);
		void Cleanup(string dir);
	}
	
	public class IOWrapper : IFileHandling
	{
		private static readonly ILogger _logger = LogContext.ForClass<IOWrapper>();

		public void MkDirIfNotExists(string path)
		{
			using var trace1 = Tracing.Trace(nameof(MkDirIfNotExists), "io")
										.WithTag("path", path);
			if (!DirExists(path))
			{
				using var trace2 = Tracing.Trace("CreateDirectory", "io")
											.WithTag("path", path);
				_logger.Debug("Creating directory {@Directory}", path);
				Directory.CreateDirectory(path);
			}
		}

		public bool DirExists(string path)
		{
			using var trace1 = Tracing.Trace(nameof(DirExists), "io")
										.WithTag("path", path);
			return Directory.Exists(path);
		}

		public bool FileExists(string path)
		{
			using var trace1 = Tracing.Trace(nameof(FileExists), "io")
										.WithTag("path", path);
			var p = Path.GetFullPath(path);
			return File.Exists(p);
		}

		public string[] GetFiles(string path)
		{
			using var trace1 = Tracing.Trace(nameof(GetFiles), "io")
										.WithTag("path", path);
			var files = Directory.GetFiles(path);

			trace1?.AddTag("numFiles", files.Length);

			return files;
		}

		public T DeserializeJson<T>(string file)
		{
			using var trace1 = Tracing.Trace(nameof(DeserializeJson), "io")
										.WithTag("path", file);

			using (var reader = new StreamReader(file))
			{
				return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
			}
		}

		public bool TryDeserializeXml<T>(string file, out T result) where T : new()
		{
			result = default;

			using var trace = Tracing.Trace(nameof(TryDeserializeXml), "io")
										.WithTag("path", file);
			if (!File.Exists(file)) return false;

			XmlSerializer serializer = new XmlSerializer(typeof(T), new XmlRootAttribute("Creator"));
			try
			{
				using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					try
					{
						result = (T)serializer.Deserialize(stream);
						return true;
					}
					catch (Exception e)
					{
						_logger.Error(e, "Failed to deserialize {@File} from xml to type {@Type}.", file, typeof(T));
						trace?.AddTag("exception.message", e.Message);
						trace?.AddTag("exception.stacktrace", e.StackTrace);
						return false;
					}
				}
		}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to read {@file}.", file);
				trace?.AddTag("exception.message", e.Message);
				trace?.AddTag("exception.stacktrace", e.StackTrace);
				return false;
			}
}

		public void MoveFailedFile(string fromPath, string toPath)
		{
			using var trace = Tracing.Trace(nameof(MoveFailedFile), "io")
										.WithTag("path.from", fromPath)
										.WithTag("path.to", toPath);
			try
			{
				MkDirIfNotExists(toPath);
				toPath = Path.Join(toPath, Path.GetFileName(fromPath));
				_logger.Debug("Moving failed file from {@FromPath} to {@ToPath}", fromPath, toPath);
				File.Copy(fromPath, toPath, overwrite: true);

			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to move file from {@FromPath} to {@ToPath}", fromPath, toPath);
				trace?.AddTag("exception.message", e.Message);
				trace?.AddTag("exception.stacktrace", e.StackTrace);
			}
		}

		public void Copy(string from, string to, bool overwrite)
		{
			using var trace1 = Tracing.Trace(nameof(Copy), "io")
										.WithTag("path.from", from)
										.WithTag("path.to", to)
										.WithTag("overwrite", overwrite.ToString());
			File.Copy(from, to, overwrite);
		}

		public bool WriteToFile(string path, string content)
		{
			using var trace = Tracing.Trace(nameof(WriteToFile), "io")
										.WithTag("path", path);
			try
			{
				File.WriteAllText(path, content);
				return true;
			} 
			catch (Exception e)
			{
				_logger.Error(e, "Failed to write content to file {@Path}", path);
				_logger.Verbose("Failed content: {@Content}", content);
				trace?.AddTag("exception.message", e.Message);
				trace?.AddTag("exception.stacktrace", e.StackTrace);
				return false;
			}
		}

		public void Cleanup(string dir)
		{
			using var trace = Tracing.Trace(nameof(Cleanup), "io")
										.WithTag("path", dir);
			if (!DirExists(dir))
				return;

			using var trace2 = Tracing.Trace("DeleteDir", "io")
											.WithTag("path", dir);

			try
			{
				_logger.Debug("Deleting directory: {@Directory}", dir);
				Directory.Delete(dir, recursive: true);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to clean up directory: {@Directory}", dir);
				trace2?.AddTag("exception.message", e.Message);
				trace2?.AddTag("exception.stacktrace", e.StackTrace);
			}
		}
	}
}
