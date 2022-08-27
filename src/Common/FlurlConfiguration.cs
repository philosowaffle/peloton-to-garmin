using Flurl.Http;
using Prometheus;
using Serilog;
using System;
using System.Threading.Tasks;
using PromMetrics = Prometheus.Metrics;
using Metrics = Common.Observe.Metrics;
using Flurl;
using System.Linq;
using System.Collections.Generic;
using Serilog.Events;
using System.Web;

namespace Common
{
	public static class FlurlConfiguration
	{
		public static readonly Histogram HttpRequestHistogram = PromMetrics.CreateHistogram("p2g_http_duration_seconds", "The histogram of http requests.", new HistogramConfiguration
		{
			LabelNames = new[]
			{
				Metrics.Label.HttpMethod,
				Metrics.Label.HttpHost,
				Metrics.Label.HttpRequestPath,
				Metrics.Label.HttpRequestQuery,
				Metrics.Label.HttpStatusCode,
				Metrics.Label.HttpMessage
			}
		});

		private static bool PrometheusEnabled = false;

		public static void Configure(Observability config, int defaultTimeoutSeconds = 10)
		{
			PrometheusEnabled = config.Prometheus.Enabled;

			Func<FlurlCall, Task> beforeCallAsync = (FlurlCall call) =>
			{
				if (Log.IsEnabled(LogEventLevel.Verbose))
					LogRequest(call, call.HttpRequestMessage?.Content?.ToString());
				return Task.CompletedTask;
			};

			Func<FlurlCall, Task> afterCallAsync = async (FlurlCall call) =>
			{
				if (Log.IsEnabled(LogEventLevel.Verbose))
					LogResponse(call, await call.HttpResponseMessage?.Content?.ReadAsStringAsync());
				TrackMetrics(call);
			};

			Func<FlurlCall, Task> onErrorAsync = (FlurlCall call) => LogErrorAsync(call);

			FlurlHttp.Configure(settings =>
			{
				settings.Timeout = new TimeSpan(0, 0, defaultTimeoutSeconds);
				settings.BeforeCallAsync = beforeCallAsync;
				settings.AfterCallAsync = afterCallAsync;
				settings.OnErrorAsync = onErrorAsync;
				settings.Redirects.ForwardHeaders = true;
			});
		}

		public static async Task LogErrorAsync(FlurlCall call)
		{
			try
			{
				var response = string.Empty;
				if (call.HttpResponseMessage is object)
					response = await call.HttpResponseMessage?.Content?.ReadAsStringAsync();
				Log.Error("Http Call Failed. {@HttpStatusCode} {@Content}",
					call.HttpResponseMessage?.StatusCode,
					response);
			}
			catch (Exception e)
			{
				Log.Information(e, "Error while trying to log http error details.");
			}
		}

		public static void LogRequest(FlurlCall call, string payload)
		{
			try
			{
				Log.Verbose("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
						call.HttpRequestMessage.Method,
						call.HttpRequestMessage.RequestUri,
						call.HttpRequestMessage.Headers.ToString(),
						payload);
			}
			catch (Exception e)
			{
				Log.Information(e, "Error while trying to log verbose request details.");
			}
		}

		public static void LogResponse(FlurlCall call, string payload)
		{
			try
			{
				Log.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
						call.HttpResponseMessage?.StatusCode,
						call.HttpRequestMessage?.Method,
						call.HttpRequestMessage?.RequestUri,
						call.HttpResponseMessage.Headers.ToString(),
						payload);
			}
			catch (Exception e)
			{
				Log.Information(e, "Error while trying to log verbose response details.");
			}
		}

		public static void TrackMetrics(FlurlCall call)
		{
			try
			{
				// Attempt to strip out dynamic values from the Metrics
				if (PrometheusEnabled)
				{
					var url = new Url(call.HttpRequestMessage.RequestUri);
					var queryParamKeys = url.QueryParams
											.Select(y => y.Name);
					var queryParamMetric = string.Join('&', queryParamKeys);

					var pathSegments = url.PathSegments;
					var metricsSegments = new List<string>(pathSegments.Count);
					foreach (var segment in pathSegments)
					{
						if (segment.Any(char.IsNumber))
							metricsSegments.Add("{dynamic}");
						else
							metricsSegments.Add(segment);
					}
					var cleansedUrl = new Url("http://temp")
										.AppendPathSegments(metricsSegments);

					TrackMetrics(call, HttpUtility.UrlDecode(cleansedUrl.Path), queryParamMetric);
				}
			} catch (Exception e)
			{
				Log.Information(e, "Error while attempting to track Http Metrics.");
			}
		}

		private static void TrackMetrics(FlurlCall call, string path, string query)
		{
			if (PrometheusEnabled)
			{
				HttpRequestHistogram
				.WithLabels(
					call.HttpRequestMessage.Method.ToString(),
					call.HttpRequestMessage.RequestUri.Host,
					path ?? string.Empty,
					query ?? string.Empty,
					((int)call.HttpResponseMessage.StatusCode).ToString(),
					call.HttpResponseMessage.ReasonPhrase
				).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
			}
		}

		public static IFlurlRequest StripSensitiveDataFromLogging(this IFlurlRequest request, string sensitiveField, string sensitiveField2 = null)
		{
			return request.ConfigureRequest((c) =>
			{
				c.BeforeCallAsync = null;
				c.BeforeCallAsync = (FlurlCall call) =>
				{
					if (Log.IsEnabled(LogEventLevel.Verbose))
					{
						var content = call.HttpRequestMessage.Content
										?.ToString()
										?.Replace(sensitiveField, "<redacted1>")
										?.Replace(sensitiveField2, "<redacted2>") ?? string.Empty;

						LogRequest(call, content);
					}
					return Task.CompletedTask;
				};

				c.AfterCallAsync = null;
				c.AfterCallAsync = async (FlurlCall call) =>
				{
					if (Log.IsEnabled(LogEventLevel.Verbose))
					{
						var content = (await call.HttpResponseMessage?.Content?.ReadAsStringAsync())
										?.Replace(sensitiveField, "<redacted1>")
										?.Replace(sensitiveField2, "<redacted2>") ?? string.Empty;

						LogResponse(call, content);
					}

					TrackMetrics(call);
				};
			});
		}
	}
}
