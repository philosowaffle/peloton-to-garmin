using Common.Stateful;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

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

		public static TracerProvider EnableConsoleTracing(Jaeger config)
		{
			TracerProvider tracing = null;
			if (!config.Enabled)
				return tracing;

			var builder = Sdk.CreateTracerProviderBuilder()
				.ConfigureDefaultBuilder(config)
				.Build();

			Log.Information("Tracing started and exporting to: http://{@Host}:{@Port}", config.AgentHost, config.AgentPort);

			return tracing;
		}

		public static void EnableApiTracing(IServiceCollection services, Jaeger config)
		{
			if (!config.Enabled)
				return;

			services.AddOpenTelemetryTracing(
				(builder) =>
				{
					builder
					.ConfigureDefaultBuilder(config)
					.AddAspNetCoreInstrumentation(c =>
					{
						c.RecordException = true;
						c.Enrich = AspNetCoreEnricher;
					});
				});

			Log.Information("Tracing started and exporting to: http://{@Host}:{@Port}", config.AgentHost, config.AgentPort);

		}

		public static void EnableWebUITracing(IServiceCollection services, Jaeger config)
		{
			if (!config.Enabled)
				return;

			services.AddOpenTelemetryTracing(
				(builder) =>
				{
					builder.ConfigureDefaultBuilder(config);
				});

			Log.Information("Tracing started and exporting to: http://{@Host}:{@Port}", config.AgentHost, config.AgentPort);
		}

		private static TracerProviderBuilder ConfigureDefaultBuilder(this TracerProviderBuilder builder, Jaeger config)
		{
			return builder
					.AddSource(Statics.TracingService)
					.SetResourceBuilder(
						ResourceBuilder
						.CreateDefault()
						.AddService(serviceName: Statics.TracingService, serviceVersion: Constants.AppVersion)
						.AddAttributes(new List<KeyValuePair<string, object>>()
						{
							new KeyValuePair<string, object>("host.machineName", Environment.MachineName),
							new KeyValuePair<string, object>("host.os", Environment.OSVersion.VersionString),
							new KeyValuePair<string, object>("dotnet.version", Environment.Version.ToString()),
						})
					)
					.SetSampler(new AlwaysOnSampler())
					.SetErrorStatusOnException()
					.AddHttpClientInstrumentation(h =>
					{
						h.RecordException = true;
						h.Enrich = HttpEnricher;
					})
					.AddJaegerExporter(o =>
					{
						o.AgentHost = config.AgentHost;
						o.AgentPort = config.AgentPort.GetValueOrDefault();
						o.Protocol = OpenTelemetry.Exporter.JaegerExportProtocol.UdpCompactThrift;
					});
		}

		public static Activity Trace(string name, string category = "app", ActivityKind kind = ActivityKind.Server)
		{
			var activity = Source?.StartActivity(name, kind);

			activity?
				.SetTag(TagKey.Category, category)
				.SetTag("SpanId", activity.SpanId)
				.SetTag("TraceId", activity.TraceId);

			return activity;
		}

		public static Activity ClientTrace(string name, string category = "app", ActivityKind kind = ActivityKind.Client)
		{
			if (Activity.Current is null || Activity.Current.Kind != ActivityKind.Client)
			{
				Activity.Current?.Dispose();
				Activity.Current = null;
				Activity.Current = Source?.CreateActivity(name, kind);
			}

			var activity = Source?.StartActivity(name, kind);

			activity?
				.SetTag(TagKey.Category, category)
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

		public static void HttpEnricher(Activity activity, string name, object rawEventObject)
		{
			if (name.Equals("OnStartActivity"))
			{
				if (rawEventObject is HttpRequestMessage request)
				{
					activity.DisplayName = $"{request.Method} {request.RequestUri.AbsolutePath}";
					activity.SetTag("http.request.path", request.RequestUri.AbsolutePath);
					activity.SetTag("http.request.query", request.RequestUri.Query);
					activity.SetTag("http.request.body", request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "no_content");
				}
			}
			else if (name.Equals("OnStopActivity"))
			{
				if (rawEventObject is HttpResponseMessage response)
				{
					var content = response.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "no_content";
					activity.SetTag("http.response.body", content);
				}
			}
			else if (name.Equals("OnException"))
			{
				if (rawEventObject is Exception exception)
				{
					activity.SetTag("stackTrace", exception.StackTrace);
				}
			}
		}

		public static void AspNetCoreEnricher(Activity activity, string name, object rawEventObject)
		{
			if (name.Equals("OnStartActivity")
				&& rawEventObject is HttpRequest httpRequest)
			{
				if (httpRequest.Headers.TryGetValue("TraceId", out var incomingTraceParent))
					activity.SetParentId(incomingTraceParent);

				if (httpRequest.Headers.TryGetValue("uber-trace-id", out incomingTraceParent))
					activity.SetParentId(incomingTraceParent);

				activity.SetTag("http.path", httpRequest.Path);
				activity.SetTag("http.query", httpRequest.QueryString);
			}
		}
	}
}
