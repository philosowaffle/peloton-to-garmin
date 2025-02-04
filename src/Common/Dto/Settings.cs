using Common.Dto.Garmin;
using Common.Stateful;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

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

	public static string DataDirectory => Statics.DefaultDataDirectory;

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

	[JsonIgnore]
	public static readonly Dictionary<WorkoutType, GarminDeviceInfo> DefaultDeviceInfoSettings = new Dictionary<WorkoutType, GarminDeviceInfo>()
	{
		{ WorkoutType.None, GarminDevices.Forerunner945 },
		{ WorkoutType.Cycling, GarminDevices.TACXDevice },
		{ WorkoutType.Rowing, GarminDevices.EpixDevice },
	};

	public bool Fit { get; set; }
	public bool Json { get; set; }
	public bool Tcx { get; set; }
	public bool SaveLocalCopy { get; set; }
	public bool IncludeTimeInHRZones { get; set; }
	public bool IncludeTimeInPowerZones { get; set; }
	[Obsolete("Use DeviceInfoSettings instead.  Will be removed in P2G v5.")]
	public string DeviceInfoPath { get; set; }
	public Dictionary<WorkoutType, GarminDeviceInfo> DeviceInfoSettings { get; set; }
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
	public GarminApiSettings Api {  get; set; } = new GarminApiSettings();
}

public class GarminApiSettings
{
	public string SsoSignInUrl { get; set; } = "https://sso.garmin.com/sso/signin";
	public string SsoEmbedUrl { get; set; } = "https://sso.garmin.com/sso/embed";
	public string SsoMfaCodeUrl { get; set; } = "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode";
	public string SsoUserAgent { get; set; } = "GCM-iOS-5.7.2.1";

	public string OAuth1TokenUrl { get; set; } = "https://connectapi.garmin.com/oauth-service/oauth/preauthorized";
	public string OAuth1LoginUrlParam { get; set; } = "https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true";

	public string OAuth2RequestUrl { get; set; } = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0";

	public string UploadActivityUrl { get; set; } = "https://connectapi.garmin.com/upload-service/upload";
	public string UploadActivityUserAgent { get; set; } = "GCM-iOS-5.7.2.1";
	public string UplaodActivityNkHeader { get; set; } = "NT";

	public string Origin { get; set; } = "https://sso.garmin.com";
	public string Referer { get; set; } = "https://sso.garmin.com/sso/signin";


}

public enum FileFormat : byte
{
	Fit = 0,
	Tcx = 1,
	Json = 2
}