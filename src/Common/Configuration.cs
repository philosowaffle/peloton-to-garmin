using System;
using System.Collections.Generic;
using System.ComponentModel;
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

	/// <summary>
	/// Configuration that must be provided prior to runtime. Typically via config file, command line args, or env variables.
	/// </summary>
	public class AppConfiguration
    {
		public Observability Observability { get; set; }
		public Developer Developer { get; set; }
    }

	/// <summary>
	/// Settings that can be looked up after app start, changed on demand, and saved to the SettingsDb.
	/// </summary>
	public class Settings
    {
		public Settings()
		{
			App = new App();
			Format = new Format();
			Peloton = new Peloton();
			Garmin = new Garmin();
		}

		public App App { get; set; }
		public Format Format { get; set; }
		public Peloton Peloton { get; set; }
		public Garmin Garmin { get; set; }
	}

	public class App
	{
		public App()
		{
			OutputDirectory = Path.Join(Environment.CurrentDirectory, "output");
			WorkingDirectory = Path.Join(Environment.CurrentDirectory, "working");
			SyncHistoryDbPath = Path.Join(OutputDirectory, "syncHistory.json");

			EnablePolling = true;
			PollingIntervalSeconds = 3600;
		}

		[DisplayName("Output Directory")]
		[Description("Where downloaded and converted files should be saved to.")] 
		public string OutputDirectory { get; set; }
		[DisplayName("Working Directory")]
		[Description("The directory where P2G can work. When running, P2G will create and delete files and needs a dedicated directory to do that.")]
		public string WorkingDirectory { get; set; }
		
		[Obsolete("Use DataDirectory as folder path.")]
		public string SyncHistoryDbPath { get; set; }
		[DisplayName("Enable Polling")]
		[Description("Enabled if you wish P2G to run continuously and poll Peloton for new workouts.")]
		public bool EnablePolling { get; set; }
		[DisplayName("Polling Interval in Seconds")]
		[Description("The polling interval in seconds determines how frequently P2G should check for new workouts. Be warned, that setting this to a frequency of hourly or less may get you flagged by Peloton as a bad actor and they may reset your password.")]
		public int PollingIntervalSeconds { get; set; }
		public bool? PythonAndGUploadInstalled { get; set; }
		public bool CloseWindowOnFinish { get; set; }

		public static string DataDirectory = Path.Join(Environment.CurrentDirectory, "data");
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

		[DisplayName("FIT")]
		[Description("Enabled indicates you wish downloaded workouts to be converted to FIT")]
		public bool Fit { get; set; }
		[DisplayName("JSON")]
		[Description("Enabled indicates you wish downloaded workouts to be converted to JSON")]
		public bool Json { get; set; }
		[DisplayName("TCX")]
		[Description("Enabled indicates you wish downloaded workouts to be converted to TCX.")]
		public bool Tcx { get; set; }
		[DisplayName("Save a local copy")]
		[Description("Save any converted workouts to your specified Output Directory")]
		public bool SaveLocalCopy { get; set; }
		[DisplayName("Include Time in HR Zones")]
		[Description("Only use this if you are unable to configure your Max HR on Garmin Connect. When set to True, P2G will attempt to capture the time spent in each HR Zone per the data returned by Peloton.")]
		public bool IncludeTimeInHRZones { get; set; }
		[DisplayName("Include Time in Power Zones")]
		[Description("Only use this if you are unable to configure your FTP and Power Zones on Garmin Connect. When set to True, P2G will attempt to capture the time spent in each Power Zone per the data returned by Peloton.")]
		public bool IncludeTimeInPowerZones { get; set; }
		[DisplayName("Device Info Path")]
		[Description("The path to your deviceInfo.xml file.")]
		public string DeviceInfoPath { get; set; }
		public Cycling Cycling { get; set; }
		public Running Running { get; set; }
	}

	public class Cycling
	{
		[DisplayName("Preferred Lap Type")]
		[Description("")]
		public PreferredLapType PreferredLapType { get; set; }
	}

	public class Running
	{
		[DisplayName("Preferred Lap Type")]
		[Description("")]
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
		[DisplayName("Number of Workouts to Download")]
		public int NumWorkoutsToDownload { get; set; }
		[DisplayName("Exclude Workout Types")]
		public ICollection<string> ExcludeWorkoutTypes { get; set; }
	}

	public class Garmin
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public bool Upload { get; set; }
		[DisplayName("Format to Upload")]
		public string FormatToUpload { get; set; }
		[DisplayName("Upload Strategy")]
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
