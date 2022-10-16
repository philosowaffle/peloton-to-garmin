using Flurl.Http.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.Wrap;

namespace Common.Http;

public class PollyHttpClientFactory : DefaultHttpClientFactory
{
	private readonly AsyncPolicyWrap<HttpResponseMessage> _policies;

	public PollyHttpClientFactory(AsyncPolicyWrap<HttpResponseMessage> policies)
	{
		_policies = policies;
	}

	public override HttpMessageHandler CreateMessageHandler()
	{
		return new PolicyHandler(_policies)
		{
			InnerHandler = base.CreateMessageHandler()
		};
	}
}

public class PolicyHandler : DelegatingHandler
{
	private readonly AsyncPolicyWrap<HttpResponseMessage> _policy;

	public PolicyHandler(AsyncPolicyWrap<HttpResponseMessage> policy)
	{
		_policy = policy;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
	}
}