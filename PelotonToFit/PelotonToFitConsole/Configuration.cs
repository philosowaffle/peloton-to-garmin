using Newtonsoft.Json;
using System;
using System.IO;

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
				File.WriteAllText(configFilePath, JsonConvert.SerializeObject(config));
				return false;
			}

			try
			{
				config = JsonConvert.DeserializeObject<Configuration>(new StreamReader(configFilePath).ReadToEnd());
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
		public Severity DebugSeverity { get; set; }
		public string OutputDirectory { get; set; }
		public string ProcessedHistoryFilePath { get; set; }
		public bool EnablePolling { get; set; }
		public int PollingIntervalSeconds { get; set; }
	}

	public class PelotonConfig
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Upload { get; set; }
	}

	public class GarminConfig
	{
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public enum Severity
	{
		None,
		Info,
		Debug
	}
}
