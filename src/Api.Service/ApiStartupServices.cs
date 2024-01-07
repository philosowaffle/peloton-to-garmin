using Common.Database;
using Common.Service;
using Common;
using Conversion;
using Garmin;
using Microsoft.Extensions.Caching.Memory;
using Peloton.AnnualChallenge;
using Peloton;
using Sync;
using Philosowaffle.Capability.ReleaseChecks;
using Microsoft.Extensions.DependencyInjection;
using Api.Services;
using Garmin.Auth;
using Api.Service;

namespace SharedStartup;

public static class ApiStartupServices
{
	public static void ConfigureP2GApiServices(this IServiceCollection services)
	{
		// CACHE
		services.AddSingleton<IMemoryCache, MemoryCache>();

		// CONVERT
		services.AddSingleton<IConverter, FitConverter>();
		services.AddSingleton<IConverter, TcxConverter>();
		services.AddSingleton<IConverter, JsonConverter>();

		// GARMIN
		services.AddSingleton<IGarminUploader, GarminUploader>();
		services.AddSingleton<IGarminApiClient, Garmin.ApiClient>();
		services.AddSingleton<IGarminAuthenticationService, GarminAuthenticationService>();

		// IO
		services.AddSingleton<IFileHandling, IOWrapper>();

		// MIGRATIONS
		services.AddSingleton<IDbMigrations, DbMigrations>();

		// PELOTON
		services.AddSingleton<IPelotonApi, Peloton.ApiClient>();
		services.AddSingleton<IPelotonService, PelotonService>();
		services.AddSingleton<IPelotonAnnualChallengeService, PelotonAnnualChallengeService>();
		services.AddSingleton<IAnnualChallengeService, AnnualChallengeService>();

		// RELEASE CHECKS
		services.AddGitHubReleaseChecker();

		// SETTINGS
		services.AddSingleton<ISettingsUpdaterService, SettingsUpdaterService>();
		services.AddSingleton<ISettingsDb, SettingsDb>();
		services.AddSingleton<ISettingsService, SettingsService>();

		// SYNC
		services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
		services.AddSingleton<ISyncService, SyncService>();

		// SYSTEM INFO
		services.AddSingleton<IVersionInformationService, VersionInformationService>();
		services.AddSingleton<ISystemInfoService, SystemInfoService>();

		// USERS
		services.AddSingleton<IUsersDb, UsersDb>();
	}
}
