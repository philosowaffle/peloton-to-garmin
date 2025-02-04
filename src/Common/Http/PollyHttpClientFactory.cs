using Flurl.Http.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.Wrap;

namespace Common.Http;

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