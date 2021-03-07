using System.Text.Json;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PelotonToFitConsole
{
	public static class ConfigurationLoader
	{
		public static bool TryLoadConfigurationFile(out Configuration config)
		{
			config = new Configuration();

			var configFilePath = Path.Join(Environment.CurrentDirectory, "configuration.local.json");
			if (!File.Exists(configFilePath))
			{
				Console.Out.WriteLine($"Failed to find file: {configFilePath}");
				Console.Out.WriteLine($"Creating file: {configFilePath}");
				File.WriteAllText(configFilePath, JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true }));
				return false;
			}

			try
			{
				config = JsonSerializer.Deserialize<Configuration>(new StreamReader(configFilePath).ReadToEnd(), new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip });
				return true;
			}
			catch(Exception e)
			{
				Console.Out.WriteLine($"Failed to parse: {configFilePath}");
				Console.Out.WriteLine($"Exception is: {e.Message}");
				Console.Out.Write(e);
				return false;
			}
		}
	}

	public class Configuration
	{
		public Configuration()
		{
			Application = new ApplicationConfig();
			Peloton = new PelotonConfig();
			Garmin = new GarminConfig();
		}

		public ApplicationConfig Application { get; set; }
		public PelotonConfig Peloton { get; set; }
		public GarminConfig Garmin { get; set; }
	}

	public class ApplicationConfig
	{
		/// <summary>
		/// Test comment.
		/// </summary>
		public Severity DebugSeverity { get; set; }
		public string OutputDirectory { get; set; }
		public string ProcessedHistoryFilePath { get; set; }
		public bool EnablePolling { get; set; }
		public int PollingIntervalSeconds { get; set; }
		public string PathToPythonExe { get; set; }

		[JsonIgnore]
		public string FitDirectory => Path.Join(OutputDirectory, "fit");
	}

	public class PelotonConfig
	{
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class GarminConfig
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Upload { get; set; }
	}

	public enum Severity
	{
		None,
		Info,
		Debug
	}
}
