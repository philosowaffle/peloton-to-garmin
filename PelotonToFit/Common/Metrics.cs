using Prometheus;
using System;
using PromMetrics = Prometheus.Metrics;

namespace Common
{
	public static class Metrics
	{
		public static readonly Counter PollsCounter = PromMetrics.CreateCounter("polls_total", "The number of times the current process has polled for new data.");

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
