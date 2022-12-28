using Common.Dto;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Common;

public static class ConfigurationSetup
{
	public static void LoadConfigValues(IConfiguration provider, Settings config)
	{
		provider.GetSection(nameof(App)).Bind(config.App);
		provider.GetSection(nameof(Format)).Bind(config.Format);
		provider.GetSection(nameof(Peloton)).Bind(config.Peloton);
		provider.GetSection(nameof(Garmin)).Bind(config.Garmin);
	}

	public static void LoadConfigValues(IConfiguration provider, AppConfiguration config)
	{
		provider.GetSection("Api").Bind(config.Api);
		provider.GetSection("WebUI").Bind(config.WebUI);
		provider.GetSection(nameof(Observability)).Bind(config.Observability);
		provider.GetSection(nameof(Developer)).Bind(config.Developer);
	}
}

/// <summary>
/// Configuration that must be provided prior to runtime. Typically via config file, command line args, or env variables.
/// </summary>
public class AppConfiguration
{
	public AppConfiguration()
	{
		Api = new ApiSettings();
		WebUI = new WebUISettings();
		Observability = new Observability();
		Developer = new Developer();
	}

	public ApiSettings Api { get; set; }
	public WebUISettings WebUI { get; set; }
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

		CheckForUpdates = true;
		EnablePolling = false;
		PollingIntervalSeconds = 86400; // 1 day
	}

	[DisplayName("Output Directory")]
	[Description("Where downloaded and converted files should be saved to.")] 
	public string OutputDirectory { get; set; }
	[DisplayName("Working Directory")]
	[Description("The directory where P2G can work. When running, P2G will create and delete files and needs a dedicated directory to do that.")]
	public string WorkingDirectory { get; set; }

	[DisplayName("Enable Polling")]
	[Description("Enabled if you wish P2G to run continuously and poll Peloton for new workouts.")]
	public bool EnablePolling { get; set; }
	[DisplayName("Polling Interval in Seconds")]
	[Description("The polling interval in seconds determines how frequently P2G should check for new workouts. Be warned, that setting this to a frequency of hourly or less may get you flagged by Peloton as a bad actor and they may reset your password.")]
	public int PollingIntervalSeconds { get; set; }
	[Obsolete]
	public bool? PythonAndGUploadInstalled { get; set; }
	public bool CloseWindowOnFinish { get; set; }
	public bool CheckForUpdates { get; set; }


	public static string DataDirectory = Path.GetFullPath(Path.Join(Environment.CurrentDirectory, "data"));
	public string FailedDirectory => Path.GetFullPath(Path.Join(OutputDirectory, "failed"));
	public string DownloadDirectory => Path.GetFullPath(Path.Join(WorkingDirectory, "downloaded"));
	public string UploadDirectory => Path.GetFullPath(Path.Join(WorkingDirectory, "upload"));

}

public class Format
{
	public Format()
	{
		Cycling = new Cycling();
		Running = new Running();
		Rowing = new Rowing();
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
	public Rowing Rowing { get; init; }
}

public record Cycling
{
	public PreferredLapType PreferredLapType { get; set; }
}

public record Running
{
	public PreferredLapType PreferredLapType { get; set; }
}

public record Rowing
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

public class Peloton : ICredentials
{
	public Peloton()
	{
		ExcludeWorkoutTypes = new List<WorkoutType>();
		NumWorkoutsToDownload = 5;
	}

	public EncryptionVersion EncryptionVersion { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public int NumWorkoutsToDownload { get; set; }
	public ICollection<WorkoutType> ExcludeWorkoutTypes { get; set; }
}

public class Garmin : ICredentials
{
	public Garmin()
	{
		UploadStrategy = UploadStrategy.NativeImplV1;
	}

	public EncryptionVersion EncryptionVersion { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
	public UploadStrategy UploadStrategy { get; set; }
}

public class ApiSettings
{
	public ApiSettings()
	{
		HostUrl = "http://localhost";
	}

	public string HostUrl { get; set; }
}

public class WebUISettings
{
	public WebUISettings()
	{
		HostUrl = "http://localhost";
	}

	public string HostUrl { get; set; }
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

public enum UploadStrategy : byte
{
	PythonAndGuploadInstalledLocally = 0,
	WindowsExeBundledPython = 1,
	NativeImplV1 = 2
}

public enum FileFormat : byte
{
	Fit = 0,
	Tcx = 1,
	Json = 2
}

public enum EncryptionVersion : byte
{
	None = 0,
	V1 = 1,
}

public interface ICredentials
{
	public EncryptionVersion EncryptionVersion { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
}