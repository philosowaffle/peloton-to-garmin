using Prometheus;
using Serilog;
using System;

namespace Common
{
	public static class Metrics
	{
		public static IMetricServer EnableMetricsServer(Prometheus config)
		{
			IMetricServer metricsServer = null;
			if (config.Enabled)
			{
				var port = config.Port ?? 4000;
				metricsServer = new KestrelMetricServer(port: port);
				metricsServer.Start();
				Log.Information("Metrics Server started and listening on: http://localhost:{0}/metrics", port);
			}

			return metricsServer;
		}

		public static bool ValidateConfig(Observability config)
		{
			if (!config.Prometheus.Enabled)
				return true;

			if (config.Prometheus.Port.HasValue && config.Prometheus.Port <= 0)
			{
				Log.Error("Prometheus Port must be a valid port: {@Config}.{@ConfigSecion}.", nameof(config), nameof(config.Prometheus.Port));
				throw new ArgumentException("Prometheus Port must be a valid port.", nameof(config.Prometheus.Port));
			}

			return true;
		}
	}
}
