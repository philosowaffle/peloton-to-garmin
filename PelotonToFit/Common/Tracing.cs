using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;

namespace Common
{
	public static class TagKey
	{
		public static string Category = "category";
		public static string App = "app";
		public static string WorkoutId = "workout_id";
		public static string Table = "table";
		public static string Format = "format";
	}

	public static class TagValue 
	{
		public static string Default = "app";
		public static string Db = "db";
		public static string Fit = "fit";
		public static string Tcx = "tcx";
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
							.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("p2g"))
							.AddHttpClientInstrumentation()
							.AddSource("P2G")
							.AddJaegerExporter(o =>
							{
								o.AgentHost = config.AgentHost;
								o.AgentPort = config.AgentPort.GetValueOrDefault();
							})
							.Build();

				Log.Information("Tracing started and exporting to: http://{0}:{1}", config.AgentHost, config.AgentPort);
			}

			return tracing;
		}

		public static Activity Trace(string name, string category = "app")
		{
			return Activity.Current?.Source.StartActivity(name)?.SetTag(TagKey.Category, category)
					?? new ActivitySource("P2G").StartActivity(name).SetTag(TagKey.Category, category);
		}

		public static Activity WithWorkoutId(this Activity activity, string workoutId)
		{
			return activity.SetTag(TagKey.WorkoutId, workoutId);
		}

		public static Activity WithTable(this Activity activity, string table)
		{
			return activity.SetTag(TagKey.Table, table);
		}

		public static bool ValidateConfig(Observability config)
		{
			if (!config.Jaeger.Enabled)
				return true;

			if (string.IsNullOrEmpty(config.Jaeger.AgentHost))
			{
				Log.Error("Agent Host must be set: {0}.{1}.", nameof(config), nameof(config.Jaeger.AgentHost));
				return false;
			}

			if (config.Jaeger.AgentPort is null || config.Jaeger.AgentPort <= 0)
			{
				Log.Error("Agent Port must be set: {0}.{1}.", nameof(config), nameof(config.Jaeger.AgentPort));
				return false;
			}

			return true;
		}
	}
}
