using Flurl.Http;
using Prometheus;
using Serilog;
using System;
using System.Threading.Tasks;
using PromMetrics = Prometheus.Metrics;
using Metrics = Common.Observe.Metrics;

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

		public static void Configure(Observability config)
		{
			Func<FlurlCall, Task> beforeCallAsync = (FlurlCall call) =>
			{
				try
				{
					Log.Verbose("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", call.HttpRequestMessage.Method, call.HttpRequestMessage.RequestUri, call.HttpRequestMessage.Headers.ToString(), call.HttpRequestMessage.Content);
				}
				catch { Console.WriteLine("Error in Flurl.beforeCallAsync"); }

				return Task.CompletedTask;
			};

			Func<FlurlCall, Task> afterCallAsync = async (FlurlCall call) =>
			{
				try
                {
					Log.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", call.HttpResponseMessage?.StatusCode, call.HttpRequestMessage?.Method, call.HttpRequestMessage?.RequestUri, call.HttpResponseMessage.Headers.ToString(), await call.HttpResponseMessage?.Content?.ReadAsStringAsync());

					if (config.Prometheus.Enabled)
					{
						HttpRequestHistogram
						.WithLabels(
							call.HttpRequestMessage.Method.ToString(),
							call.HttpRequestMessage.RequestUri.Host,
							call.HttpRequestMessage.RequestUri.AbsolutePath,
							call.HttpRequestMessage.RequestUri.Query,
							((int)call.HttpResponseMessage.StatusCode).ToString(),
							call.HttpResponseMessage.ReasonPhrase
						).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
					}
				} catch { Console.WriteLine("Error in Flurl.afterCallAsync"); }				
			};

			Func<FlurlCall, Task> onErrorAsync = async (FlurlCall call) =>
			{
				try
                {
					var response = string.Empty;
					if (call.HttpResponseMessage is object)
						response = await call.HttpResponseMessage?.Content?.ReadAsStringAsync();
					Log.Error("Http Call Failed. {@HttpStatusCode} {@Content}", call.HttpResponseMessage?.StatusCode, response);
				}
				catch { Console.WriteLine("Error in Flurl.onErrorAsync"); }
			};

			FlurlHttp.Configure(settings =>
			{
				settings.Timeout = new TimeSpan(0, 0, 10);
				settings.BeforeCallAsync = beforeCallAsync;
				settings.AfterCallAsync = afterCallAsync;
				settings.OnErrorAsync = onErrorAsync;
				settings.Redirects.ForwardHeaders = true;
			});
		}
	}
}
