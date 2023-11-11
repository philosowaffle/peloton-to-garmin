using Common.Dto;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
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
		CheckForUpdates = true;
		EnablePolling = false;
		PollingIntervalSeconds = 86400; // 1 day
	}

	public bool EnablePolling { get; set; }
	public int PollingIntervalSeconds { get; set; }
	public bool CheckForUpdates { get; set; }

	public static string DataDirectory => Path.GetFullPath(Path.Join(Statics.DefaultDataDirectory, "data"));

	public string WorkingDirectory => Statics.DefaultTempDirectory;
	public string OutputDirectory => Statics.DefaultOutputDirectory;
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
		Strength= new Strength();
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
	public Rowing Rowing { get; init; }
	public Strength Strength { get; init; }
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

public record Strength
{
	/// <summary>
	/// When no Rep information is provided by Peloton, P2G will calculate number
	/// of reps based on this default value. Example, if your DefaultNumSecondsPerRep is 3,
	/// and the Exercise duration was 15 seconds, then P2G would credit you with 5 reps for that
	/// exercise.
	/// </summary>
	public int DefaultSecondsPerRep { get; set; } = 3;
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
	public EncryptionVersion EncryptionVersion { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public bool TwoStepVerificationEnabled { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
}

public class ApiSettings
{
	public ApiSettings()
	{
		HostUrl = "http://*:8080";
	}

	public string HostUrl { get; set; }
}

public class WebUISettings
{
	public WebUISettings()
	{
		HostUrl = "http://*:8080";
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