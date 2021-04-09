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
		private static readonly Counter HttpResponseCounter = PromMetrics.CreateCounter("p2g_http_responses", "The number of http responses.", new CounterConfiguration
		{
			LabelNames = new[] { "method", "host", "path", "query", "status_code", "duration_in_seconds" }
		});
		private static readonly Counter HttpErrorCounter = PromMetrics.CreateCounter("p2g_http_errors", "The number of errors encountered.", new CounterConfiguration
		{
			LabelNames = new[] { "method", "host", "path", "query", "status_code", "duration_in_seconds", "message" }
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
					HttpResponseCounter
					.WithLabels(
						call.HttpRequestMessage.Method.ToString(),
						call.HttpRequestMessage.RequestUri.Host,
						call.HttpRequestMessage.RequestUri.AbsolutePath,
						call.HttpRequestMessage.RequestUri.Query,
						call.HttpResponseMessage.StatusCode.ToString(),
						call.Duration.GetValueOrDefault().TotalSeconds.ToString()
					)
					.Inc();
				}
			};

			Func<FlurlCall, Task> onErrorAsync = async (FlurlCall call) =>
			{
				Log.Error("Http Call Failed. {0} {1}", call.HttpResponseMessage?.StatusCode, await call.HttpResponseMessage?.Content?.ReadAsStringAsync());

				if (config.Observability.Prometheus.Enabled)
				{
					HttpErrorCounter
					.WithLabels(
						call.HttpRequestMessage.Method.ToString(),
						call.HttpRequestMessage.RequestUri.Host,
						call.HttpRequestMessage.RequestUri.AbsolutePath,
						call.HttpRequestMessage.RequestUri.Query,
						call.HttpResponseMessage.StatusCode.ToString(),
						call.Duration.GetValueOrDefault().TotalSeconds.ToString(),
						call.HttpResponseMessage.ReasonPhrase
					)
					.Inc();
				}
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
