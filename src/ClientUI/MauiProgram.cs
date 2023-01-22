using ClientUI.Data;
using Common.Http;
using Common.Observe;
using Common;
using Microsoft.Extensions.Logging;
using Philosowaffle.Capability.ReleaseChecks;
using Serilog;
using SharedUI;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using Havit.Blazor.Components.Web;
using Common.Service;
using Common.Database;
using Microsoft.Extensions.Caching.Memory;
using Conversion;
using Garmin;
using Peloton;
using Peloton.AnnualChallenge;
using Sync;

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

		// CACHE
		builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

		// CONVERT
		builder.Services.AddSingleton<IConverter, FitConverter>();
		builder.Services.AddSingleton<IConverter, TcxConverter>();
		builder.Services.AddSingleton<IConverter, JsonConverter>();

		// GARMIN
		builder.Services.AddSingleton<IGarminUploader, GarminUploader>();
		builder.Services.AddSingleton<IGarminApiClient, Garmin.ApiClient>();

		// IO
		builder.Services.AddSingleton<IFileHandling, IOWrapper>();

		// HAVIT
		builder.Services.AddHxServices();
		builder.Services.AddHxMessenger();

		// MIGRATIONS
		builder.Services.AddSingleton<IDbMigrations, DbMigrations>();

		// PELOTON
		builder.Services.AddSingleton<IPelotonApi, Peloton.ApiClient>();
		builder.Services.AddSingleton<IPelotonService, PelotonService>();
		builder.Services.AddSingleton<IAnnualChallengeService, AnnualChallengeService>();

		// RELEASE CHECKS
		builder.Services.AddGitHubReleaseChecker();

		// SETTINGS
		builder.Services.AddSingleton<ISettingsDb, SettingsDb>();
		builder.Services.AddSingleton<ISettingsService, SettingsService>();

		// SYNC
		builder.Services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
		builder.Services.AddSingleton<ISyncService, SyncService>();

		// USERS
		builder.Services.AddSingleton<IUsersDb, UsersDb>();

		FlurlConfiguration.Configure(config.Observability, 30);
		Tracing.EnableWebUITracing(builder.Services, config.Observability.Jaeger);

		Log.Logger = new LoggerConfiguration()
						.ReadFrom.Configuration(builder.Configuration, sectionName: $"{nameof(Observability)}:Serilog")
						.Enrich.FromLogContext()
						.CreateLogger();

		Logging.LogSystemInformation();

		///////////////////////////////////////////////////////////
		/// APP
		///////////////////////////////////////////////////////////

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
	builder.Logging.AddDebug();
#endif

		builder.Services.AddSingleton<WeatherForecastService>();

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);

		return builder.Build();
	}
}