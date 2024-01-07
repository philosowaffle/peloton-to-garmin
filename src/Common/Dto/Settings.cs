using Common.Stateful;
using System.Collections.Generic;
using System.IO;

namespace Common.Dto;

/// <summary>
/// Settings that can be looked up after app start, changed on demand, and saved to the SettingsDb.
/// </summary>
public class Settings
{
	public Settings()
	{
		App = new ();
		Format = new ();
		Peloton = new ();
		Garmin = new ();
	}

	public App App { get; set; }
	public Format Format { get; set; }
	public PelotonSettings Peloton { get; set; }
	public GarminSettings Garmin { get; set; }
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
		Strength = new Strength();
	}

	public bool Fit { get; set; }
	public bool Json { get; set; }
	public bool Tcx { get; set; }
	public bool SaveLocalCopy { get; set; }
	public bool IncludeTimeInHRZones { get; set; }
	public bool IncludeTimeInPowerZones { get; set; }
	public string DeviceInfoPath { get; set; }
	public string WorkoutTitleTemplate { get; set; } = "{{PelotonWorkoutTitle}}{{#if PelotonInstructorName}} with {{PelotonInstructorName}}{{/if}}";
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

public class PelotonSettings : ICredentials
{
	public PelotonSettings()
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

public class GarminSettings : ICredentials
{
	public EncryptionVersion EncryptionVersion { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public bool TwoStepVerificationEnabled { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
}

public enum FileFormat : byte
{
	Fit = 0,
	Tcx = 1,
	Json = 2
}