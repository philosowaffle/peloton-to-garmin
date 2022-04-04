namespace Api.Contracts;

public class SystemInfoGetResponse
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
}
