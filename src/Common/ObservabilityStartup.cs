using Common.Http;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.Span;
using System.IO;

namespace Common;

public static class ObservabilityStartup
{
	public static void ConfigureClientUI(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		FlurlConfiguration.Configure(config.Observability);
		ConfigureLogging(configManager, hardcodeFileLogging:true);

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);
	}

	public static void ConfigureApi(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableApiTracing(services, config.Observability.Jaeger);
		ConfigureLogging(configManager);

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);
	}

	public static void ConfigureWebUI(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableWebUITracing(services, config.Observability.Jaeger);
		ConfigureLogging(configManager);

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);
	}

	private static void ConfigureLogging(ConfigurationManager configManager, bool hardcodeFileLogging = false)
	{
		var loggingConfig = new LoggerConfiguration()
						.ReadFrom.Configuration(configManager, sectionName: $"{nameof(Observability)}:Serilog")
						.Enrich.WithSpan()
						.Enrich.FromLogContext();

		if (hardcodeFileLogging)
			loggingConfig.WriteTo.File(Path.Join(Statics.DefaultOutputDirectory, "log.txt"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);

		Log.Logger = loggingConfig.CreateLogger();


		Logging.LogSystemInformation();
	}
}
