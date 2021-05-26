using Common;
using Common.Database;
using Conversion;
using Garmin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Peloton;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Metrics = Prometheus.Metrics;

namespace PelotonToGarminConsole
{
	class Program
	{
		private static readonly Histogram SyncHistogram = Metrics.CreateHistogram("p2g_sync_duration_seconds", "The histogram of sync jobs that have run.");
		private static readonly Gauge P2GBuildInfo = Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Common.Metrics.Label.Version, Common.Metrics.Label.Os, Common.Metrics.Label.OsVersion, Common.Metrics.Label.DotNetRuntime }
		});

		static void Main(string[] args)
		{
			Console.WriteLine("Peloton To Garmin");
			var config = new Configuration();

			var runtimeVersion = Environment.Version.ToString();
			var os = Environment.OSVersion.Platform.ToString();
			var osVersion = Environment.OSVersion.VersionString;
			var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

			try
			{
				IConfiguration configProviders = new ConfigurationBuilder()
				.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();
								
				configProviders.GetSection(nameof(App)).Bind(config.App);
				configProviders.GetSection(nameof(Format)).Bind(config.Format);
				configProviders.GetSection(nameof(Peloton)).Bind(config.Peloton);
				configProviders.GetSection(nameof(Garmin)).Bind(config.Garmin);
				configProviders.GetSection(nameof(Observability)).Bind(config.Observability);
				configProviders.GetSection(nameof(Developer)).Bind(config.Developer);

				// https://github.com/serilog/serilog-settings-configuration
				Log.Logger = new LoggerConfiguration()
					.ReadFrom.Configuration(configProviders, sectionName: $"{nameof(Observability)}:Serilog")
					.Enrich.WithSpan()
					.CreateLogger();

				ChangeToken.OnChange(() => configProviders.GetReloadToken(), () => 
				{
					Log.Information("Config change detected, reloading config values.");
					configProviders.GetSection(nameof(App)).Bind(config.App);
					configProviders.GetSection(nameof(Format)).Bind(config.Format);
					configProviders.GetSection(nameof(Peloton)).Bind(config.Peloton);
					configProviders.GetSection(nameof(Garmin)).Bind(config.Garmin);
					configProviders.GetSection(nameof(Developer)).Bind(config.Developer);

					GarminUploader.ValidateConfig(config.Garmin);

					Log.Information("Config reloaded. Changes will take effect at the end of the current sleeping cycle.");
				});

				Log.Debug("P2G Version: {@Version}", version);
				Log.Debug("Operating System: {@Os}", osVersion);
				Log.Debug("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);

				PelotonService.ValidateConfig(config.Peloton);
				GarminUploader.ValidateConfig(config.Garmin);
				Common.Metrics.ValidateConfig(config.Observability);
				Tracing.ValidateConfig(config.Observability);
				
				FlurlConfiguration.Configure(config);

			} catch (Exception e)
			{
				Log.Error(e, "Exception during config setup.");
				throw;
			}			

			try
			{
				using var metrics = Common.Metrics.EnableMetricsServer(config.Observability.Prometheus);
				using var tracing = Tracing.EnableTracing(config.Observability.Jaeger);
				using var tracingSource = new ActivitySource("ROOT");

				P2GBuildInfo.WithLabels(version, os, osVersion, runtimeVersion).Set(1);

				if (config.Peloton.NumWorkoutsToDownload <= 0)
				{
					Console.Write("How many workouts to grab? ");
					int num = Convert.ToInt32(Console.ReadLine());
					config.Peloton.NumWorkoutsToDownload = num;
				}

				if (config.App.EnablePolling)
				{
					while (config.App.EnablePolling)
					{
						RunAsync(config).GetAwaiter().GetResult();
						Log.Information("Sleeping for {@Seconds} seconds...", config.App.PollingIntervalSeconds);
						Thread.Sleep(config.App.PollingIntervalSeconds * 1000);
					}
				}
				else
				{
					RunAsync(config).GetAwaiter().GetResult();
				}
			}
			catch (Exception e)
			{
				Log.Error(e, "Uncaught Exception.");
				throw;
			}
			finally
			{
				Log.CloseAndFlush();
				Console.ReadLine();
			}
		}

		static async Task RunAsync(Configuration config)
		{
			using var timer = SyncHistogram.NewTimer();
			using var activity = Tracing.Trace(nameof(RunAsync));

			var fileHandler = new IOWrapper();
			var db = new DbClient(config, fileHandler);
			var pelotonApiClient = new Peloton.ApiClient(config.Peloton.Email, config.Peloton.Password, config.Observability.Prometheus.Enabled);
			var peloton = new PelotonService(config, pelotonApiClient, db, fileHandler);

			await peloton.DownloadLatestWorkoutDataAsync();

			var fitConverter = new FitConverter(config, db, fileHandler);
			fitConverter.Convert();

			var tcxConverter = new TcxConverter(config, db, fileHandler);
			tcxConverter.Convert();

			var garminUploader = new GarminUploader(config);
			garminUploader.UploadToGarmin();

			fileHandler.Cleanup(config.App.DownloadDirectory);
			fileHandler.Cleanup(config.App.UploadDirectory);
			foreach (var file in Directory.GetFiles(config.App.WorkingDirectory))
				File.Delete(file);
		}
	}
}
