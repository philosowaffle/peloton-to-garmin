
namespace PelotonToFitConsole
{
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
