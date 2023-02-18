using Common.Observe;
using Common;
using Microsoft.Extensions.Logging;
using SharedUI;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using SharedStartup;

namespace ClientUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		///////////////////////////////////////////////////////////
		/// STATICS
		///////////////////////////////////////////////////////////
		Statics.AppType = Constants.ClientUIName;
		Statics.MetricPrefix = Constants.ClientUIName;
		Statics.TracingService = Constants.ClientUIName;

		Statics.DefaultDataDirectory = FileSystem.Current.AppDataDirectory;
		Statics.DefaultOutputDirectory = FileSystem.Current.AppDataDirectory;
		Statics.DefaultTempDirectory = FileSystem.Current.CacheDirectory;

		Directory.CreateDirectory(Path.Combine(Statics.DefaultOutputDirectory, "output"));

		///////////////////////////////////////////////////////////
		/// HOST
		///////////////////////////////////////////////////////////
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		var configProvider = builder.Configuration.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: "P2G_");

		var config = new AppConfiguration();
		ConfigurationSetup.LoadConfigValues(builder.Configuration, config);

		///////////////////////////////////////////////////////////
		/// SERVICES
		///////////////////////////////////////////////////////////

		// API CLIENT
		builder.Services.AddSingleton<IApiClient, ServiceClient>();

		builder.Services.ConfigureSharedUIServices();
		builder.Services.ConfigureP2GApiServices();

		ObservabilityStartup.Configure(builder.Services, builder.Configuration, config);

		///////////////////////////////////////////////////////////
		/// APP
		///////////////////////////////////////////////////////////

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);

		return builder.Build();
	}
}