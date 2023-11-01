using Flurl.Http;
using System;

namespace Peloton;

public static class Extensions
{
	public static void EnsurePelotonCredentialsAreProvided(this Common.Peloton settings)
	{
		if (string.IsNullOrEmpty(settings.Email))
			throw new ArgumentException("Peloton Email must be set.", nameof(settings.Email));

		if (string.IsNullOrEmpty(settings.Password))
			throw new ArgumentException("Peloton Password must be set.", nameof(settings.Password));
	}

	public static IFlurlRequest WithCommonHeaders(this IFlurlRequest request)
	{
		return request
			.WithHeader("Peloton-Platform", "web"); // needed to get GPS points for outdoor activity in response
	}
}
