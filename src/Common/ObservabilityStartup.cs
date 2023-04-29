using Common.Http;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;

namespace Common;

public static class ObservabilityStartup
{
	public static void Configure(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config, bool hardcodeFileLogging = false)
	{
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableWebUITracing(services, config.Observability.Jaeger);

		var loggingConfig = new LoggerConfiguration()
						.ReadFrom.Configuration(configManager, sectionName: $"{nameof(Observability)}:Serilog");

		if (hardcodeFileLogging)
			loggingConfig.WriteTo.File(Path.Join(Statics.DefaultOutputDirectory, "log.txt"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);

		Log.Logger = loggingConfig.CreateLogger();


		Logging.LogSystemInformation();
	}
}
