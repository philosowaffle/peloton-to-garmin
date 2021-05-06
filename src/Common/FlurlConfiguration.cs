using Flurl.Http;
using Prometheus;
using Serilog;
using System;
using System.Threading.Tasks;
using PromMetrics = Prometheus.Metrics;

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

		public static void Configure(Configuration config)
		{
			Func<FlurlCall, Task> beforeCallAsync = (FlurlCall call) =>
			{
				Log.Debug("{0} {1} {2}", call.HttpRequestMessage.Method, call.HttpRequestMessage.RequestUri, call.HttpRequestMessage.Content);
				return Task.CompletedTask;
			};

			Func<FlurlCall, Task> afterCallAsync = async (FlurlCall call) =>
			{
				Log.Debug("{0} {1}", call.HttpResponseMessage?.StatusCode, await call.HttpResponseMessage?.Content?.ReadAsStringAsync());

				if (config.Observability.Prometheus.Enabled)
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
			};

			Func<FlurlCall, Task> onErrorAsync = async (FlurlCall call) =>
			{
				Log.Error("Http Call Failed. {0} {1}", call.HttpResponseMessage?.StatusCode, await call.HttpResponseMessage?.Content?.ReadAsStringAsync());
			};

			FlurlHttp.Configure(settings =>
			{
				settings.Timeout = new TimeSpan(0, 0, 10);
				settings.BeforeCallAsync = beforeCallAsync;
				settings.AfterCallAsync = afterCallAsync;
				settings.OnErrorAsync = onErrorAsync;
			});
		}
	}
}
