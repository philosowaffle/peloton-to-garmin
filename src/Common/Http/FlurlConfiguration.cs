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
using System.Net;
using Polly;

namespace Common.Http;

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

		Func<FlurlCall, Task> beforeCallAsync = (call) =>
		{
			if (Log.IsEnabled(LogEventLevel.Verbose))
				LogRequest(call, call.GetRawRequestBody());
			return Task.CompletedTask;
		};

		Func<FlurlCall, Task> afterCallAsync = async (call) =>
		{
			if (Log.IsEnabled(LogEventLevel.Verbose))
				LogResponse(call, await call.GetRawResponseBodyAsync());
			TrackMetrics(call);
		};

		Func<FlurlCall, Task> onErrorAsync = async (call) => {
			var request = call.GetRawRequestBody();
			var response = await call.GetRawResponseBodyAsync();
			LogError(call, request, response);
		};

		FlurlHttp.Configure(settings =>
		{
			settings.Timeout = new TimeSpan(0, 0, defaultTimeoutSeconds);
			settings.BeforeCallAsync = beforeCallAsync;
			settings.AfterCallAsync = afterCallAsync;
			settings.OnErrorAsync = onErrorAsync;
			settings.Redirects.ForwardHeaders = true;
		});

		FlurlHttp.ConfigureClient("https://api.onepeloton.com", client =>
		{
			var policies = Policy.WrapAsync(PollyPolicies.Retry, PollyPolicies.NoOp);
			client.Settings.HttpClientFactory = new PollyHttpClientFactory(policies);
		});
	}

	public static void LogError(FlurlCall call, string requestPayload, string responsePayload)
	{
		try
		{
			if (call.Exception is FlurlParsingException fpe)
			{
				Log.Error(fpe, $"Http Failed to deserialize response to target type: {fpe.ExpectedFormat}");
				Log.Information("Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
					call.HttpRequestMessage?.Method?.Method,
					call.HttpRequestMessage?.RequestUri,
					call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
					responsePayload);
				return;
			}

			if (call.Exception is FlurlHttpTimeoutException hte)
			{
				Log.Error(hte, $"Http Timeout: {hte.Message}");
				return;
			}

			if (call.Exception is object)
			{
				Log.Information("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpRequestMessage.Method?.Method,
					call.HttpRequestMessage.RequestUri,
					call.HttpRequestMessage.Headers.ToString(),
					requestPayload);
				Log.Information("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
					call.HttpRequestMessage?.Method?.Method,
					call.HttpRequestMessage?.RequestUri,
					call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
					responsePayload);
				return;
			}

			Log.Information("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
				call.HttpRequestMessage.Method?.Method,
				call.HttpRequestMessage.RequestUri,
				call.HttpRequestMessage.Headers.ToString(),
				requestPayload);
			Log.Information("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
				call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
				call.HttpRequestMessage?.Method?.Method,
				call.HttpRequestMessage?.RequestUri,
				call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
				responsePayload);
			return;
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
					call.HttpRequestMessage.Method?.Method,
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
					call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
					call.HttpRequestMessage?.Method?.Method,
					call.HttpRequestMessage?.RequestUri,
					call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
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
		}
		catch (Exception e)
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
				call.HttpRequestMessage.Method.ToString() ?? string.Empty,
				call.HttpRequestMessage.RequestUri?.Host ?? string.Empty,
				path ?? string.Empty,
				query ?? string.Empty,
				((int?)call.HttpResponseMessage?.StatusCode)?.ToString() ?? string.Empty,
				call.HttpResponseMessage?.ReasonPhrase ?? string.Empty
			).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
		}
	}

	public static IFlurlRequest StripSensitiveDataFromLogging(this IFlurlRequest request, string sensitiveField, string sensitiveField2 = null)
	{
		return request.ConfigureRequest((c) =>
		{
			c.BeforeCallAsync = null;
			c.BeforeCallAsync = (call) =>
			{
				if (Log.IsEnabled(LogEventLevel.Verbose))
				{
					var content = call.GetRawRequestBody()
									?.Replace(sensitiveField, "<redacted1>")
									?.Replace(sensitiveField2, "<redacted2>") ?? string.Empty;

					LogRequest(call, content);
				}
				return Task.CompletedTask;
			};

			c.AfterCallAsync = null;
			c.AfterCallAsync = async (call) =>
			{
				if (Log.IsEnabled(LogEventLevel.Verbose))
				{
					var content = (await call.GetRawResponseBodyAsync())
									?.Replace(sensitiveField, "<redacted1>")
									?.Replace(sensitiveField2, "<redacted2>") ?? string.Empty;

					LogResponse(call, content);
				}

				TrackMetrics(call);
			};

			c.OnErrorAsync = null;
			c.OnErrorAsync = async (call) =>
			{
				var requestContent = call.GetRawRequestBody()
									?.ToString()
									?.Replace(sensitiveField, "<redacted1>")
									?.Replace(sensitiveField2, "<redacted2>") ?? string.Empty;

				var responseContent = (await call.GetRawResponseBodyAsync())
								?.Replace(sensitiveField, "<redacted1>")
								?.Replace(sensitiveField2, "<redacted2>") ?? string.Empty;

				LogError(call, requestContent, responseContent);
			};
		});
	}

	private static string GetRawRequestBody(this FlurlCall call)
	{
		var request = string.Empty;
		if (call.HttpRequestMessage is object
			&& call.HttpRequestMessage.Content is object)
			request = call.HttpRequestMessage.Content.ToString() ?? string.Empty;

		return request;
	}

	private static async Task<string> GetRawResponseBodyAsync(this FlurlCall call)
	{
		var responseBody = string.Empty;
		if (call.HttpResponseMessage is object
			&& call.HttpResponseMessage.Content is object)
			responseBody = await call.HttpResponseMessage.Content.ReadAsStringAsync() ?? string.Empty;

		return responseBody;
	}
}
