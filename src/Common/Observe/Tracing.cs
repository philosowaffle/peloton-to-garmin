using Common.Stateful;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Diagnostics;

namespace Common.Observe
{
	public static class TagKey
	{
		public static string Category = "category";
		public static string App = "app";
		public static string WorkoutId = "workout_id";
		public static string Table = "table";
		public static string Format = "file_type";
	}

	public static class TagValue
	{
		public static string Default = "app";
		public static string Db = "db";
		public static string Fit = "fit";
		public static string Tcx = "tcx";
		public static string P2G = "p2g";
	}

	public static class Tracing
	{
		public static ActivitySource Source;

		public static TracerProvider EnableTracing(Jaeger config)
		{
			TracerProvider tracing = null;
			if (config.Enabled)
			{
				tracing = Sdk.CreateTracerProviderBuilder()
							.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Statics.MetricPrefix))
							.AddHttpClientInstrumentation(config =>
							{
								config.RecordException = true;
								config.Enrich = (activity, name, rawEventObject) =>
								{
									activity.SetTag(TagKey.App, TagValue.P2G);
									activity.SetTag("SpanId", activity.SpanId);
									activity.SetTag("TraceId", activity.TraceId);
								};
							})
							.AddSource(Statics.MetricPrefix)
							.AddJaegerExporter(o =>
							{
								o.AgentHost = config.AgentHost;
								o.AgentPort = config.AgentPort.GetValueOrDefault();
							})
							.Build();

				Log.Information("Tracing started and exporting to: http://{@Host}:{@Port}", config.AgentHost, config.AgentPort);
			}

			return tracing;
		}

		public static Activity Trace(string name, string category = "app")
		{
			var activity = Activity.Current?.Source.StartActivity(name)
				??
				new ActivitySource(Statics.MetricPrefix)?.StartActivity(name);

			activity?
				.SetTag(TagKey.Category, category)
				.SetTag(TagKey.App, TagValue.P2G)
				.SetTag("SpanId", activity.SpanId)
				.SetTag("TraceId", activity.TraceId);

			return activity;
		}

		public static Activity WithWorkoutId(this Activity activity, string workoutId)
		{
			return activity?.SetTag(TagKey.WorkoutId, workoutId);
		}

		public static Activity WithTable(this Activity activity, string table)
		{
			return activity?.SetTag(TagKey.Table, table);
		}

		public static Activity WithTag(this Activity activity, string key, string value)
		{
			return activity?.SetTag(key, value);
		}

		public static void ValidateConfig(Observability config)
		{
			if (!config.Jaeger.Enabled)
				return;

			if (string.IsNullOrEmpty(config.Jaeger.AgentHost))
			{
				Log.Error("Agent Host must be set: {@ConfigSection}.{@ConfigProperty}.", nameof(config), nameof(config.Jaeger.AgentHost));
				throw new ArgumentException("Agent Host must be set.", nameof(config.Jaeger.AgentHost));
			}

			if (config.Jaeger.AgentPort is null || config.Jaeger.AgentPort <= 0)
			{
				Log.Error("Agent Port must be set: {@ConfigSection}.{@ConfigProperty}.", nameof(config), nameof(config.Jaeger.AgentPort));
				throw new ArgumentException("Agent Port must be a valid port.", nameof(config.Jaeger.AgentPort));
			}
		}
	}
}
