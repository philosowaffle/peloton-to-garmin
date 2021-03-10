using System.Text.Json;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Common
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
				Console.Out.WriteLine();
				Console.Out.WriteLine("***Please modify the file listed above with your configuration values and run the application again.***");
				Console.Out.WriteLine();
				File.WriteAllText(configFilePath, JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true }));
				return false;
			}

			try
			{
				using (var reader = new StreamReader(configFilePath))
				{
					config = JsonSerializer.Deserialize<Configuration>(reader.ReadToEnd(), new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip });
				}
				
				// write data back to the file so that any newly added config values exist for the future
				if (config.DoNotEdit_ConfigVersion != Configuration.CurrentConfigVersion)
				{
					config.DoNotEdit_ConfigVersion = Configuration.CurrentConfigVersion;
					File.WriteAllText(configFilePath, JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true }));
				}

				return true;
			}
			catch (Exception e)
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
		[JsonIgnore]
		public static readonly int CurrentConfigVersion = 1;

		public Configuration()
		{
			Application = new ApplicationConfig() 
			{
				SyncHistoryDbPath = Path.Join(Environment.CurrentDirectory, "syncHistory.db")
			};

			Peloton = new PelotonConfig();
			Garmin = new GarminConfig();

			_configVersion = 0;
		}

		public ApplicationConfig Application { get; set; }
		public PelotonConfig Peloton { get; set; }
		public GarminConfig Garmin { get; set; }

		private int? _configVersion = null;
		public int DoNotEdit_ConfigVersion 
		{
			get { return _configVersion ?? CurrentConfigVersion; }
			set { _configVersion = value; }
		}
	}

	public class ApplicationConfig
	{
		/// <summary>
		/// Test comment.
		/// </summary>
		public Severity DebugSeverity { get; set; }
		public string OutputDirectory { get; set; }
		public string SyncHistoryDbPath { get; set; }
		public bool EnablePolling { get; set; }
		public int PollingIntervalSeconds { get; set; }

		[JsonIgnore]
		public string FitDirectory => Path.Join(OutputDirectory, "fit");
	}

	public class PelotonConfig
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public int NumWorkoutsToDownload { get; set; }
	}

	public class GarminConfig
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Upload { get; set; }
		public bool IgnoreSyncHistory { get; set; }
	}

	public enum Severity
	{
		None,
		Info,
		Debug
	}
}
