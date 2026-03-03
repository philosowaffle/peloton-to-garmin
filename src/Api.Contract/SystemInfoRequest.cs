using Serilog.Events;

namespace Api.Contract;

public record SystemInfoGetRequest
{
	/// <summary>
	/// Whether or not to check if a new P2G update is available.
	/// When False, NewerVersionAvailable and LatestVersionInformation will be null in the response.
	/// </summary>
	public bool CheckForUpdate { get; set; }
}

public record SystemInfoGetResponse
{
	public SystemInfoGetResponse()
	{
		RunTimeVersion = string.Empty;
		OperatingSystem = string.Empty;
		OperatingSystemVersion = string.Empty;
		Version = string.Empty;
		GitHub = string.Empty;
		Documentation = string.Empty;
		Forums = string.Empty;
		Donate = string.Empty;
		Issues = string.Empty;
		Api = string.Empty;
		OutputDirectory = string.Empty;
		TempDirectory = string.Empty;

		LatestVersionInformation = new ();
	}

	public string RunTimeVersion { get; set; }
	public string OperatingSystem { get; set; }
	public string OperatingSystemVersion { get; set; }
	public string Version { get; set; }
	public string GitHub { get; set; }
	public string Documentation { get; set; }
	public string Forums { get; set; }
	public string Donate { get; set; }
	public string Issues { get; set; }
	public string Api { get; set; }
	public bool? NewerVersionAvailable { get; set; }
	public LatestVersionInformation? LatestVersionInformation { get; set; }
	public string OutputDirectory { get; set; }
	public string TempDirectory { get; set; }
	public string ApplicationConfigFilePath { get; set; } = string.Empty;
	public LogEventLevel CurrentLogLevel { get; set; }
}

public class LatestVersionInformation
{
	public LatestVersionInformation()
	{
		LatestVersion = string.Empty;
		ReleaseUrl = string.Empty;
		ReleaseDate = string.Empty;
		Description = string.Empty;
	}

	public string? LatestVersion { get; set; }
	public string? ReleaseDate { get; set; }
	public string? ReleaseUrl { get; set; }
	public string? Description { get; set; }
}


public class SystemInfoLogsGetResponse
{
	public string? LogText { get; set; }
}

public record LogLevelPostRequest
{
	public LogEventLevel LogLevel { get; set; }
}
public record LogLevelPostResponse
{
	public LogEventLevel LogLevel { get; set; }
}