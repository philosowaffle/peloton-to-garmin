using Flurl.Http;
using System;
using System.Threading.Tasks;

namespace PelotonToFitConsole
{
	public static class FlurlConfiguration
	{
		public static void Configure()
		{
			FlurlHttp.Configure(settings =>
			{
				settings.BeforeCallAsync = BeforeCallAsync;
				settings.AfterCallAsync = AfterCallAsync;
			});
		}

		private static async Task BeforeCallAsync(FlurlCall call)
		{
			if (Configuration.DebugSeverity == Severity.Debug)
				Console.Out.WriteLine($"{call.HttpRequestMessage.Method} {call.HttpRequestMessage.RequestUri} {call.HttpRequestMessage.Content}");
		}

		private static async Task AfterCallAsync(FlurlCall call)
		{
			if (Configuration.DebugSeverity == Severity.Debug)
				Console.Out.WriteLine($"{call.HttpResponseMessage.StatusCode} {await call.HttpResponseMessage.Content.ReadAsStringAsync()}");
		}
	}
}
