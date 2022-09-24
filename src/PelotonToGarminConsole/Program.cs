using Common;
using Common.Database;
using Common.Service;
using Common.Observe;
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
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Caching.Memory;

Console.WriteLine("Welcome! P2G is starting up...");

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

			configBuilder
				.AddJsonFile(Path.Join(configPath, "configuration.local.json"), optional: true, reloadOnChange: true)
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
			services.AddSingleton<AppConfiguration>((serviceProvider) =>
			{
				var config = new AppConfiguration();
				var provider = serviceProvider.GetService<IConfiguration>();
				if (provider is null) return config;

				ConfigurationSetup.LoadConfigValues(provider, config);

				ChangeToken.OnChange(() => provider.GetReloadToken(), () =>
				{
					Log.Information("Config change detected, reloading config values.");
					ConfigurationSetup.LoadConfigValues(provider, config);
					try
					{
						Metrics.ValidateConfig(config.Observability);
						Tracing.ValidateConfig(config.Observability);
					}
					catch (Exception e)
					{
						Log.Error(e, "Configuration invalid");
					}

					Log.Information("Config reloaded.");
				});

				return config;
			});

			// CACHE
			services.AddSingleton<IMemoryCache, MemoryCache>();

			// SETTINGS
			services.AddSingleton<ISettingsDb, SettingsDb>();
			services.AddSingleton<ISettingsService>((serviceProvider) =>
			{
				var configurationLoader = serviceProvider.GetService<IConfiguration>();
				var settingService = new SettingsService(serviceProvider.GetService<ISettingsDb>(), serviceProvider.GetService<IMemoryCache>());
				return new FileBasedSettingsService(configurationLoader, settingService);
			});
			services.AddSingleton<Settings>((serviceProvider) =>
			{
				var config = new Settings();
				var provider = serviceProvider.GetService<IConfiguration>();
				var settingsService = serviceProvider.GetService<ISettingsService>();
				if (provider is null) return config;
				if (settingsService is null) return config;

				config = settingsService.GetSettingsAsync().Result;

				ChangeToken.OnChange(() => provider.GetReloadToken(), () =>
				{
					Log.Information("Config change detected, reloading config values.");
					config = settingsService.GetSettingsAsync().Result;

					try
					{
						PelotonService.ValidateConfig(config.Peloton);
						GarminUploader.ValidateConfig(config);
					}
					catch (Exception e)
					{
						Log.Error(e, "Configuration invalid");
					}

					Log.Information("Config reloaded.");
				});

				return config;
			});

			// IO
			services.AddSingleton<IFileHandling, IOWrapper>();

			// PELOTON
			services.AddSingleton<IPelotonApi, Peloton.ApiClient>();
			services.AddSingleton<IPelotonService, PelotonService>();

			// GARMIN
			services.AddSingleton<IGarminUploader, GarminUploader>();

			// SYNC
			services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
			services.AddSingleton<ISyncService, SyncService>();

			// CONVERT
			services.AddSingleton<IConverter, FitConverter>();
			services.AddSingleton<IConverter, TcxConverter>();
			services.AddSingleton<IConverter, JsonConverter>();

			var config = new AppConfiguration();
			ConfigurationSetup.LoadConfigValues(hostContext.Configuration, config);

			FlurlConfiguration.Configure(config.Observability);

			services.AddHostedService<Startup>();
		});
}