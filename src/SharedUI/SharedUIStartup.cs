using Havit.Blazor.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace SharedUI;

public static class SharedUIStartup
{
	public static void ConfigureSharedUIServices(this IServiceCollection services)
	{
		services.AddHxServices();
		services.AddHxMessenger();
	}
}
