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

			services.AddSingleton<Settings>((serviceProvider) => 
			{
				var config = new Settings();
				var provider = serviceProvider.GetService<IConfiguration>();
				if (provider is null) return config;

				ConfigurationSetup.LoadConfigValues(provider, config);

				ChangeToken.OnChange(() => provider.GetReloadToken(), () =>
				{
					Log.Information("Config change detected, reloading config values.");
					ConfigurationSetup.LoadConfigValues(provider, config);

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

			services.AddSingleton<ISettingsDb, SettingsDb>();
			services.AddSingleton<ISettingsService, SettingsService>();

			services.AddSingleton<IFileHandling, IOWrapper>();
			services.AddSingleton<IPelotonApi, Peloton.ApiClient>();
			services.AddSingleton<IPelotonService, PelotonService>();
			services.AddSingleton<IGarminUploader, GarminUploader>();

			services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
			services.AddSingleton<ISyncService, SyncService>();

			services.AddSingleton<IConverter, FitConverter>();
			services.AddSingleton<IConverter, TcxConverter>();
			services.AddSingleton<IConverter, JsonConverter>();


			var config = new AppConfiguration();
			ConfigurationSetup.LoadConfigValues(hostContext.Configuration, config);

			FlurlConfiguration.Configure(config.Observability);

			services.AddHostedService<Startup>();
		});
}