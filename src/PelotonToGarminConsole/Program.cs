using Common;
using Common.Database;
using Common.Service;
using Conversion;
using Garmin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Peloton;
using PelotonToGarminConsole;
using System;
using System.IO;
using Sync;
using Serilog;
using Serilog.Enrichers.Span;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Common.Http;
using Common.Stateful;
using Philosowaffle.Capability.ReleaseChecks;
using Garmin.Auth;

Statics.AppType = Constants.ConsoleAppName;
Statics.MetricPrefix = Constants.ConsoleAppName;
Statics.TracingService = Constants.ConsoleAppName;

using IHost host = CreateHostBuilder(args).Build();
await host.RunAsync();

static IHostBuilder CreateHostBuilder(string[] args)
{
	return Host.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration(configBuilder =>
		{
			configBuilder.Sources.Clear();

			var configPath = Environment.CurrentDirectory;
			if (args.Length > 0) configPath = args[0];

			Statics.ConfigPath = Path.Join(configPath, "configuration.local.json");

			configBuilder
				.AddJsonFile(Statics.ConfigPath, optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: $"{Constants.EnvironmentVariablePrefix}_")
				.AddCommandLine(args)
				.Build();

		})
		.UseSerilog((ctx, logConfig) =>
		{
			logConfig
				.ReadFrom.Configuration(ctx.Configuration, sectionName: "Observability:Serilog")
				.Enrich.WithSpan()
				.Enrich.FromLogContext();
		})
		.ConfigureServices((hostContext, services) =>
		{
			// CACHE
			services.AddSingleton<IMemoryCache, MemoryCache>();

			// IO
			services.AddSingleton<IFileHandling, IOWrapper>();

			// SETTINGS
			services.AddSingleton<ISettingsDb, SettingsDb>();
			services.AddSingleton<ISettingsService>((serviceProvider) =>
			{
				var settingService = new SettingsService(serviceProvider.GetService<ISettingsDb>(), serviceProvider.GetService<IMemoryCache>(), serviceProvider.GetService<IConfiguration>(), serviceProvider.GetService<IFileHandling>());
				var memCache = serviceProvider.GetService<IMemoryCache>();
				var fileHandler = serviceProvider.GetService<IFileHandling>();
				return new FileBasedSettingsService(serviceProvider.GetService<IConfiguration>(), settingService, memCache, fileHandler);
			});

			// PELOTON
			services.AddSingleton<IPelotonApi, Peloton.ApiClient>();
			services.AddSingleton<IPelotonService, PelotonService>();

			// GARMIN
			services.AddSingleton<IGarminAuthenticationService, GarminAuthenticationService>();
			services.AddSingleton<IGarminUploader, GarminUploader>();
			services.AddSingleton<IGarminApiClient, Garmin.ApiClient>();

			// RELEASE CHECKS
			services.AddGitHubReleaseChecker();

			// SYNC
			services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
			services.AddSingleton<ISyncService, SyncService>();

			// CONVERT
			services.AddSingleton<IConverter, FitConverter>();
			services.AddSingleton<IConverter, TcxConverter>();
			services.AddSingleton<IConverter, JsonConverter>();

			// HTTP
			var config = new AppConfiguration();
			ConfigurationSetup.LoadConfigValues(hostContext.Configuration, config);
			FlurlConfiguration.Configure(config.Observability);

			services.AddHostedService<Startup>();
		});
}