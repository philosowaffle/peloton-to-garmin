using Common.Http;
using Common.Observe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common;

public static class ObservabilityStartup
{
	public static void Configure(IServiceCollection services, ConfigurationManager configManager, AppConfiguration config)
	{
		FlurlConfiguration.Configure(config.Observability);
		Tracing.EnableWebUITracing(services, config.Observability.Jaeger);

		Log.Logger = new LoggerConfiguration()
						.ReadFrom.Configuration(configManager, sectionName: $"{nameof(Observability)}:Serilog")
						.Enrich.FromLogContext()
						.CreateLogger();

		Logging.LogSystemInformation();
	}
}
