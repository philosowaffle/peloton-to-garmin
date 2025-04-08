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
using Common.Observe;
using Common.Service;

namespace Common.Http;

public static class FlurlConfiguration
{
	private static readonly ILogger _logger = LogContext.ForStatic(nameof(FlurlConfiguration));
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
	private static ISettingsService SettingsService;

	public static void Configure(Observability config, ISettingsService settingsService, int defaultTimeoutSeconds = 10)
	{
		PrometheusEnabled = config.Prometheus.Enabled;
		SettingsService = settingsService;

		Func<FlurlCall, Task> beforeCallAsync = async (call) =>
		{
			try
			{
				if (_logger.IsEnabled(LogEventLevel.Verbose))
				{
					var settings = await SettingsService.GetSettingsAsync();
					var pelotonAuth = SettingsService.GetPelotonApiAuthentication(settings.Peloton.Email);
					var content = call.GetRawRequestBody().StripSensitiveData(settings.Peloton.Email, settings.Peloton.Password, pelotonAuth?.SessionId, settings.Garmin.Email, settings.Garmin.Password);
					LogRequest(call, content);
				}
			} catch (Exception e)
			{
				_logger.Error("Failed to write verbose http request logs.", e);
			}
		};

		Func<FlurlCall, Task> afterCallAsync = async (call) =>
		{
			try
			{
				if (_logger.IsEnabled(LogEventLevel.Verbose))
				{
					var settings = await SettingsService.GetSettingsAsync();
					var content = (await call.GetRawResponseBodyAsync()).StripSensitiveData(settings.Peloton.Email, settings.Peloton.Password, settings.Garmin.Email, settings.Garmin.Password);
					LogResponse(call, content);
				}
			}
			catch (Exception e)
			{
				_logger.Error("Failed to write verbose http response logs.", e);
			}


			TrackMetrics(call);
		};

		Func<FlurlCall, Task> onErrorAsync = async (call) => 
		{
			try
			{
				var settings = await SettingsService.GetSettingsAsync();
				var request = call.GetRawRequestBody().StripSensitiveData(settings.Peloton.Email, settings.Peloton.Password, settings.Garmin.Email, settings.Garmin.Password);
				var response = (await call.GetRawResponseBodyAsync()).StripSensitiveData(settings.Peloton.Email, settings.Peloton.Password, settings.Garmin.Email, settings.Garmin.Password);
				LogError(call, request, response);
			}
			catch (Exception e)
			{
				_logger.Error("Failed to write verbose http error logs.", e);
			}
		};

		FlurlHttp.Clients.WithDefaults(builder =>
		{
			builder.WithTimeout(new TimeSpan(0, 0, defaultTimeoutSeconds))
			.BeforeCall(beforeCallAsync)
			.AfterCall(afterCallAsync)
			.OnError(onErrorAsync)
			.WithAutoRedirect(true);

			builder.Settings.Redirects.ForwardAuthorizationHeader = true;
		});

		FlurlHttp.ConfigureClientForUrl("https://api.onepeloton.com")
			.AddMiddleware(() => 
			{
				var policies = Policy.WrapAsync(PollyPolicies.Retry, PollyPolicies.NoOp);
				return new PolicyHandler(policies);
			});
	}

	public static void LogError(FlurlCall call, string requestPayload, string responsePayload)
	{
		try
		{
			if (call.Exception is FlurlParsingException fpe)
			{
				_logger.Error(fpe, $"Http Failed to deserialize response to target type: {fpe.ExpectedFormat}");
				_logger.Information("Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
					call.HttpRequestMessage?.Method?.Method,
					call.HttpRequestMessage?.RequestUri,
					call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
					responsePayload);
				return;
			}

			if (call.Exception is FlurlHttpTimeoutException hte)
			{
				_logger.Error(hte, $"Http Timeout: {hte.Message}");
				return;
			}

			if (call.Exception is object)
			{
				_logger.Information("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpRequestMessage.Method?.Method,
					call.HttpRequestMessage.RequestUri,
					call.HttpRequestMessage.Headers.ToString(),
					requestPayload);
				_logger.Information("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
					call.HttpRequestMessage?.Method?.Method,
					call.HttpRequestMessage?.RequestUri,
					call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
					responsePayload);
				return;
			}

			_logger.Information("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
				call.HttpRequestMessage.Method?.Method,
				call.HttpRequestMessage.RequestUri,
				call.HttpRequestMessage.Headers.ToString(),
				requestPayload);
			_logger.Information("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
				call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
				call.HttpRequestMessage?.Method?.Method,
				call.HttpRequestMessage?.RequestUri,
				call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
				responsePayload);
			return;
		}
		catch (Exception e)
		{
			_logger.Information(e, "Error while trying to log http error details.");
		}
	}

	public static void LogRequest(FlurlCall call, string payload)
	{
		try
		{
			_logger.Verbose("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpRequestMessage.Method?.Method,
					call.HttpRequestMessage.RequestUri,
					call.HttpRequestMessage.Headers.ToString(),
					payload);
		}
		catch (Exception e)
		{
			_logger.Information(e, "Error while trying to log verbose request details.");
		}
	}

	public static void LogResponse(FlurlCall call, string payload)
	{
		try
		{
			_logger.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
					call.HttpResponseMessage?.StatusCode ?? (HttpStatusCode)0,
					call.HttpRequestMessage?.Method?.Method,
					call.HttpRequestMessage?.RequestUri,
					call.HttpResponseMessage?.Headers?.ToString() ?? string.Empty,
					payload);
		}
		catch (Exception e)
		{
			_logger.Information(e, "Error while trying to log verbose response details.");
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
			_logger.Information(e, "Error while attempting to track Http Metrics.");
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

	private static string StripSensitiveData(this string content, params string[] sensitiveFields)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(content)) return string.Empty;
			foreach (var sensitiveField in sensitiveFields)
			{
				if (!string.IsNullOrEmpty(sensitiveField))
					content = content?.Replace(sensitiveField, "<redacted>") ?? string.Empty;
			}
		} catch (Exception e)
		{
			_logger.Error("Failed to strip sensitive data from Http payload.", e);
		}

		return content ?? string.Empty;
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
