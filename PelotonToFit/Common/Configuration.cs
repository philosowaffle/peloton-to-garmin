using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Common
{
	public class Configuration
	{
		public Configuration()
		{
			App = new App();
			Peloton = new Peloton();
			Garmin = new Garmin();
			Observability = new Observability();
		}

		public App App { get; set; }
		public Peloton Peloton { get; set; }
		public Garmin Garmin { get; set; }

		public Observability Observability { get; set; }
	}

	public class App
	{
		public App()
		{
			OutputDirectory = Path.Join(Environment.CurrentDirectory, "output");
			SyncHistoryDbPath = Path.Join(Environment.CurrentDirectory, "syncHistory.json");

			EnablePolling = true;
			PollingIntervalSeconds = 3600;
		}

		public string OutputDirectory { get; set; }
		
		public string SyncHistoryDbPath { get; set; }
		public bool EnablePolling { get; set; }
		public int PollingIntervalSeconds { get; set; }

		[JsonIgnore]
		public string FitDirectory => Path.Join(OutputDirectory, "fit");
	}

	public class Peloton
	{
		public Peloton()
		{
			NumWorkoutsToDownload = 5;
		}

		public string Email { get; set; }
		public string Password { get; set; }
		public int NumWorkoutsToDownload { get; set; }
	}

	public class Garmin
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Upload { get; set; }
		public bool IgnoreSyncHistory { get; set; }
	}

	public class Observability
	{
		public Observability()
		{
			Prometheus = new Prometheus();
			Jaeger = new Jaeger();
			LogLevel = Severity.None;
		}

		public Severity LogLevel { get; set; }

		public Prometheus Prometheus { get; set; }
		public Jaeger Jaeger { get; set; }
	}

	public class Jaeger
	{
		public bool Enabled { get; set; }
		public string AgentHost { get; set; }
		public int? AgentPort { get; set; }
	}

	public class Prometheus
	{
		public bool Enabled { get; set; }
		public int? Port { get; set; }
	}

	public enum Severity
	{
		None,
		Info,
		Debug
	}
}
