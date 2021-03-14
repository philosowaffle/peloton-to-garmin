﻿using Flurl.Http;
using System;
using System.Threading.Tasks;

namespace Common
{
	public static class FlurlConfiguration
	{
		public static void Configure(Configuration config)
		{
			Func<FlurlCall, Task> beforeCallAsync = delegate (FlurlCall call)
			{
				if (config.Application.DebugSeverity == Severity.Debug)
					Console.Out.WriteLine($"{call.HttpRequestMessage.Method} {call.HttpRequestMessage.RequestUri} {call.HttpRequestMessage.Content}");
				return Task.CompletedTask;
			};

			Func<FlurlCall, Task> afterCallAsync = async delegate (FlurlCall call)
			{
				if (config.Application.DebugSeverity == Severity.Debug)
					Console.Out.WriteLine($"{call.HttpResponseMessage.StatusCode} {await call.HttpResponseMessage.Content.ReadAsStringAsync()}");

				if (config.Observability.Prometheus.Enabled)
				{
					Metrics.HttpResponseCounter
					.WithLabels(
						call.HttpRequestMessage.Method.ToString(),
						call.HttpRequestMessage.RequestUri.Host.ToString(),
						call.HttpRequestMessage.RequestUri.AbsolutePath.ToString(),
						call.HttpRequestMessage.RequestUri.Query.ToString(),
						call.HttpResponseMessage.StatusCode.ToString(),
						call.Duration.GetValueOrDefault().TotalSeconds.ToString()
					)
					.Inc();
				}
			};

			Func<FlurlCall, Task> onErrorAsync = async delegate (FlurlCall call)
			{
				Console.Out.WriteLine($"HTTP Call Failed.");
				Console.Out.WriteLine($"{call.HttpResponseMessage.StatusCode} {await call.HttpResponseMessage.Content.ReadAsStringAsync()}");

				if (config.Observability.Prometheus.Enabled)
				{
					Metrics.HttpErrorCounter
					.WithLabels(
						call.HttpRequestMessage.Method.ToString(),
						call.HttpRequestMessage.RequestUri.Host.ToString(),
						call.HttpRequestMessage.RequestUri.AbsolutePath.ToString(),
						call.HttpRequestMessage.RequestUri.Query.ToString(),
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
