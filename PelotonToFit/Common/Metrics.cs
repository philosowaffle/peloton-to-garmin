using Prometheus;
using System;
using System.Collections.Generic;
using PromMetrics = Prometheus.Metrics;

namespace Common
{
	public static class Metrics
	{
		public static readonly Counter PollsCounter = PromMetrics.CreateCounter("polls_total", "The number of times the current process has polled for new data.");

		public static readonly Counter HttpResponseCounter = PromMetrics.CreateCounter("http_responses", "The number of http responses.", new CounterConfiguration
		{
			LabelNames = new[] { "method", "uri", "status_code", "duration_in_seconds" }
		});

		public static readonly Counter HttpErrorCounter = PromMetrics.CreateCounter("http_errors", "The number of errors encountered.", new CounterConfiguration
		{
			LabelNames = new[] { "method", "uri", "status_code", "duration_in_seconds", "message" }
		});

		public static IMetricServer EnableMetricsServer(Configuration config)
		{
			IMetricServer metricsServer = null;
			if (config.Observability.Prometheus.Enabled)
			{
				var port = config.Observability.Prometheus.Port ?? 4000;
				var registry = new CollectorRegistry();
				var staticLabels = new Dictionary<string, string>() { { "app", "p2g" } };
				registry.SetStaticLabels(staticLabels);
				metricsServer = new KestrelMetricServer(port: port, registry: registry);
				metricsServer.Start();
				Console.Out.WriteLine($"Metrics Server started and listening on: http://localhost:{port}/metrics");
			}

			return metricsServer;
		}

		public static bool ValidateConfig(ObservabilityConfig config)
		{
			if (!config.Prometheus.Enabled)
				return true;

			if (config.Prometheus.Port.HasValue && config.Prometheus.Port <= 0)
			{
				Console.Out.WriteLine($"Prometheus Port must be a valid port: {nameof(config)}.{nameof(config.Prometheus.Port)}.");
				return false;
			}

			return true;
		}
	}
}
