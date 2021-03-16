using Prometheus;
using System;
using PromMetrics = Prometheus.Metrics;

namespace Common
{
	public static class Metrics
	{
		// GENERAL
		public static readonly Counter PollsCounter = PromMetrics.CreateCounter("polls_total", "The number of times the current process has polled for new data.");
		public static readonly Histogram PollDuration = PromMetrics.CreateHistogram("poll_duration_seconds", "Histogram of the entire poll run duration.");

		// WORKOUT
		public static readonly Histogram WorkoutConversionDuration = PromMetrics.CreateHistogram("workout_conversion_duration_seconds", "Histogram of workout conversion durations.", new HistogramConfiguration() 
		{
			LabelNames = new[] { "format" }
		});
		public static readonly Gauge WorkoutsToConvert = PromMetrics.CreateGauge("workout_conversion_pending", "The number of workouts pending conversion to output format.");
		public static readonly Histogram WorkoutUploadDuration = PromMetrics.CreateHistogram("workout_upload_duration_seconds", "Histogram of workout upload durations.", new HistogramConfiguration()
		{
			LabelNames = new[] { "count" }
		});

		// HTTP
		public static readonly Counter HttpResponseCounter = PromMetrics.CreateCounter("http_responses", "The number of http responses.", new CounterConfiguration
		{
			LabelNames = new[] { "method", "host", "path", "query", "status_code", "duration_in_seconds" }
		});
		public static readonly Counter HttpErrorCounter = PromMetrics.CreateCounter("http_errors", "The number of errors encountered.", new CounterConfiguration
		{
			LabelNames = new[] { "method", "host", "path", "query", "status_code", "duration_in_seconds", "message" }
		});

		// DB
		public static readonly Histogram DbActionDuration = PromMetrics.CreateHistogram("db_action_duration_seconds", "Histogram of db action durations.", new HistogramConfiguration()
		{
			LabelNames = new[] { "action", "queryName" }
		});

		public static IMetricServer EnableMetricsServer(Prometheus config)
		{
			IMetricServer metricsServer = null;
			if (config.Enabled)
			{
				var port = config.Port ?? 4000;
				metricsServer = new KestrelMetricServer(port: port);
				metricsServer.Start();
				Console.Out.WriteLine($"Metrics Server started and listening on: http://localhost:{port}/metrics");
			}

			return metricsServer;
		}

		public static bool ValidateConfig(Observability config)
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
