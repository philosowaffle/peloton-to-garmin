using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

namespace Common
{
	public static class Tracing
	{
		public static ActivitySource Source;

		public static string Category = "category";
		public static string Route = "route";
		public static string App = "app";
		public static string WorkoutId = "workout_id";
		public static string Table = "table";
		public static string Format = "format";

		public static string Default = "default";
		public static string Db = "db";
		public static string Http = "http";
		public static string Fit = "fit";

		public static TracerProvider EnableTracing(Jaeger config)
		{
			TracerProvider tracing = null;
			if (config.Enabled)
			{
				tracing = Sdk.CreateTracerProviderBuilder()
							.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("p2g"))
							.AddSource("P2G.Root")
							.AddJaegerExporter(o =>
							{
								o.AgentHost = config.AgentHost;
								o.AgentPort = config.AgentPort.GetValueOrDefault();
							})
							.Build();

				Console.Out.WriteLine($"Tracing started and exporting to: http://{config.AgentHost}:{config.AgentPort}");
			}

			return tracing;
		}

		public static bool ValidateConfig(Observability config)
		{
			if (!config.Jaeger.Enabled)
				return true;

			if (string.IsNullOrEmpty(config.Jaeger.AgentHost))
			{
				Console.Out.WriteLine($"Agent Host must be set: {nameof(config)}.{nameof(config.Jaeger.AgentHost)}.");
				return false;
			}

			if (config.Jaeger.AgentPort is null || config.Jaeger.AgentPort <= 0)
			{
				Console.Out.WriteLine($"Agent Port must be set: {nameof(config)}.{nameof(config.Jaeger.AgentPort)}.");
				return false;
			}

			return true;
		}
	}
}
