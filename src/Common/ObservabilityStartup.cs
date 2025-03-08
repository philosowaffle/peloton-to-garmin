using Common.Http;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Settings.Configuration;
using System.IO;

namespace Common;

public static class ObservabilityStartup
{
	public static void ConfigureClientUI(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		ConfigureLogging(configManager);
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableWebUITracing(services, config.Observability.Jaeger);

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);
	}

	public static void ConfigureApi(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		ConfigureLogging(configManager);
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableApiTracing(services, config.Observability.Jaeger);

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);
	}

	public static void ConfigureWebUI(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		ConfigureLogging(configManager);
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableWebUITracing(services, config.Observability.Jaeger);

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);
	}

	private static void ConfigureLogging(ConfigurationManager configManager)
	{
		Logging.InternalLevelSwitch = new Serilog.Core.LoggingLevelSwitch();

		var options = new ConfigurationReaderOptions
		{
			SectionName = $"{nameof(Observability)}:Serilog"
		};

		var loggingConfig = new LoggerConfiguration()
						.ReadFrom.Configuration(configManager, options)
						.MinimumLevel.ControlledBy(Logging.InternalLevelSwitch)
						.Enrich.WithSpan()
						.Enrich.FromLogContext();

		// Always write to app defined log file
		loggingConfig.WriteTo.File(
				Path.Join(Statics.DefaultOutputDirectory, $"{Statics.AppType}_log.txt"), 
				rollingInterval: RollingInterval.Day,
				retainedFileCountLimit: 2,
				shared: false,
				hooks: new CaptureFilePathHook(),
				levelSwitch: Logging.InternalLevelSwitch);

		Log.Logger = loggingConfig.CreateLogger();

		Logging.LogSystemInformation();
	}
}
