using Common.Dto;
using Microsoft.Extensions.Configuration;

namespace Common;

public static class ConfigurationSetup
{
	public static void LoadConfigValues(IConfiguration provider, Settings config)
	{
		provider.GetSection(nameof(App)).Bind(config.App);
		provider.GetSection(nameof(Format)).Bind(config.Format);
		provider.GetSection("Peloton").Bind(config.Peloton);
		provider.GetSection("Garmin").Bind(config.Garmin);
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