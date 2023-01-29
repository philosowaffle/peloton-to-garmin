using Flurl.Http.Configuration;
using System.Net.Http;

namespace Common.Http;

public class TestingHttpClientFactory : DefaultHttpClientFactory
{
	public override HttpMessageHandler CreateMessageHandler()
	{
		return new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (_, _, _, _) => true
		};
	}
}