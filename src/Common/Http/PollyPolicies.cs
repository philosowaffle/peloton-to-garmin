using Polly;
using Polly.Retry;
using System;
using System.Net.Http;
using Flurl.Http;
using Serilog;
using Polly.NoOp;

namespace Common.Http;

public static class PollyPolicies
{
	public static AsyncNoOpPolicy<HttpResponseMessage> NoOp = Policy.NoOpAsync<HttpResponseMessage>();

	public static AsyncRetryPolicy<HttpResponseMessage> Retry = Policy
		.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.Unauthorized)
		.Or<FlurlHttpTimeoutException>()
		.WaitAndRetryAsync(new[]
		{
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(5)
		},
		(result, timeSpan, retryCount, context) =>
		{
			Log.Information("Retry Policy - {@Url} - attempt {@Attempt}", result?.Result?.RequestMessage?.RequestUri, retryCount);
		});
}
