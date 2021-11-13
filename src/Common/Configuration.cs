using System;
using System.Collections.Generic;
using System.IO;

namespace Common
{
	public interface IAppConfiguration 
	{
		App App { get; set; }
		Format Format { get; set; }
		Peloton Peloton { get; set; }
		Garmin Garmin { get; set; }

		Observability Observability { get; set; }
		Developer Developer { get; set; }
	}

	public class Configuration : IAppConfiguration
	{
		public Configuration()
		{
			App = new App();
			Format = new Format();
			Peloton = new Peloton();
			Garmin = new Garmin();
			Observability = new Observability();
			Developer = new Developer();
		}

		public App App { get; set; }
		public Format Format { get; set; }
		public Peloton Peloton { get; set; }
		public Garmin Garmin { get; set; }

		public Observability Observability { get; set; }
		public Developer Developer { get; set; }
	}

	public class App
	{
		public App()
		{
			OutputDirectory = Path.Join(Environment.CurrentDirectory, "output");
			WorkingDirectory = Path.Join(Environment.CurrentDirectory, "working");
			SyncHistoryDbPath = Path.Join(OutputDirectory, "syncHistory.json");
			ConfigDbPath = Path.Join(OutputDirectory, "config_db.json");

			EnablePolling = true;
			PollingIntervalSeconds = 3600;
		}

		public string OutputDirectory { get; set; }
		public string WorkingDirectory { get; set; }
		
		public string SyncHistoryDbPath { get; set; }
		public string ConfigDbPath { get; set; }
		public bool EnablePolling { get; set; }
		public int PollingIntervalSeconds { get; set; }
		public bool? PythonAndGUploadInstalled { get; set; }
		public bool CloseWindowOnFinish { get; set; }

		public string FitDirectory => Path.Join(OutputDirectory, "fit");
		public string JsonDirectory => Path.Join(OutputDirectory, "json");
		public string TcxDirectory => Path.Join(OutputDirectory, "tcx");
		public string FailedDirectory => Path.Join(OutputDirectory, "failed");
		public string DownloadDirectory => Path.Join(WorkingDirectory, "downloaded");
		public string UploadDirectory => Path.Join(WorkingDirectory, "upload");

	}

	public class Format
	{
		public Format()
		{
			Cycling = new Cycling();
			Running = new Running();
		}

		public bool Fit { get; set; }
		public bool Json { get; set; }
		public bool Tcx { get; set; }
		public bool SaveLocalCopy { get; set; }
		public bool IncludeTimeInHRZones { get; set; }
		public bool IncludeTimeInPowerZones { get; set; }
		public string DeviceInfoPath { get; set; }
		public Cycling Cycling { get; set; }
		public Running Running { get; set; }
	}

	public class Cycling
	{
		public PreferredLapType PreferredLapType { get; set; }
	}

	public class Running
	{
		public PreferredLapType PreferredLapType { get; set; }
	}

	public enum PreferredLapType
	{
		Default = 0,
		Distance = 1,
		Class_Segments = 2,
		Class_Targets = 3
	}

	public class Peloton
	{
		public Peloton()
		{
			ExcludeWorkoutTypes = new List<string>();
			NumWorkoutsToDownload = 5;
		}

		public string Email { get; set; }
		public string Password { get; set; }
		public int NumWorkoutsToDownload { get; set; }
		public ICollection<string> ExcludeWorkoutTypes { get; set; }
	}

	public class Garmin
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Upload { get; set; }
		public string FormatToUpload { get; set; }
		public UploadStrategy UploadStrategy { get; set; }
	}

	public class Observability
	{
		public Observability()
		{
			Prometheus = new Prometheus();
			Jaeger = new Jaeger();
		}

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

	public class Developer
	{
		public string UserAgent { get; set; }
	}

	public enum UploadStrategy
	{
		PythonAndGuploadInstalledLocally = 0,
		WindowsExeBundledPython = 1,
		NativeImplV1 = 2
	}
}
