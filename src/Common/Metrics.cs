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

		public static void ValidateConfig(Observability config)
		{
			if (!config.Prometheus.Enabled) return;

			if (config.Prometheus.Port.HasValue && config.Prometheus.Port <= 0)
			{
				Log.Error("Prometheus Port must be a valid port: {@ConfigSection}.{@ConfigProperty}.", nameof(config), nameof(config.Prometheus.Port));
				throw new ArgumentException("Prometheus port must be greater than 0.", nameof(config.Prometheus.Port));
			}
		}
	}
}
