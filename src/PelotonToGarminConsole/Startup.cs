using Common;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Garmin;
using Microsoft.Extensions.Hosting;
using Peloton;
using Prometheus;
using Serilog;
using Sync;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Common.Observe.Metrics;
using Metrics = Common.Observe.Metrics;

namespace PelotonToGarminConsole
{
	internal class Startup : BackgroundService
	{
		private static readonly ILogger _logger = LogContext.ForClass<Startup>();
		private static readonly Gauge BuildInfo = Prometheus.Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Metrics.Label.Version, Metrics.Label.Os, Metrics.Label.OsVersion, Metrics.Label.DotNetRuntime }
		});
		private static readonly Gauge Health = Prometheus.Metrics.CreateGauge("p2g_health_info", "Health status for P2G.");
		private static readonly Gauge NextSyncTime = Prometheus.Metrics.CreateGauge("p2g_next_sync_time", "The next time the sync will run in seconds since epoch.");

		private readonly ISettingsService _settingsService;
		private readonly ISyncService _syncService;

		public Startup(ISettingsService settingsService, ISyncService syncService)
		{
			_settingsService = settingsService;
			_syncService = syncService;

			var runtimeVersion = Environment.Version.ToString();
			var os = Environment.OSVersion.Platform.ToString();
			var osVersion = Environment.OSVersion.VersionString;
			var version = Constants.AppVersion;

			BuildInfo.WithLabels(version, os, osVersion, runtimeVersion).Set(1);
			_logger.Information("App Version: {@Version}", version);
			_logger.Information("Operating System: {@Os}", osVersion);
			_logger.Information("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);
		}

		protected override async Task ExecuteAsync(CancellationToken cancelToken)
		{
			_logger.Verbose("Begin.");

			var settings = await _settingsService.GetSettingsAsync();
			var appConfig = await _settingsService.GetAppConfigurationAsync();

			try
			{
				PelotonService.ValidateConfig(settings.Peloton);
				GarminUploader.ValidateConfig(settings);
				Metrics.ValidateConfig(appConfig.Observability);
				Tracing.ValidateConfig(appConfig.Observability);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Exception during config validation. Please modify your configuration.local.json and relaunch the application.");
				Health.Set(HealthStatus.Dead);
				if (!settings.App.CloseWindowOnFinish)
					Console.ReadLine();
				Environment.Exit(-1);
			}

			Health.Set(HealthStatus.Healthy);
			await RunAsync(cancelToken);
		}

		private async Task RunAsync(CancellationToken cancelToken)
		{
			int exitCode = 0;

			Statics.MetricPrefix = Constants.ConsoleAppName;
			Statics.TracingService = Constants.ConsoleAppName;

			var appConfig = await _settingsService.GetAppConfigurationAsync();

			using var metrics = Metrics.EnableMetricsServer(appConfig.Observability.Prometheus);
			using var metricsCollector = Metrics.EnableCollector(appConfig.Observability.Prometheus);
			using var tracing = Tracing.EnableTracing(appConfig.Observability.Jaeger);
			using var tracingSource = new ActivitySource("ROOT");

			var settings = await _settingsService.GetSettingsAsync();

			try
			{
				if (settings.Peloton.NumWorkoutsToDownload <= 0)
				{
					Console.Write("How many workouts to grab? ");
					int num = Convert.ToInt32(Console.ReadLine());
					settings.Peloton.NumWorkoutsToDownload = num;
				}

				if (settings.App.EnablePolling)
				{
					while (settings.App.EnablePolling && !cancelToken.IsCancellationRequested)
					{
						var syncResult = await _syncService.SyncAsync(settings.Peloton.NumWorkoutsToDownload);
						Health.Set(syncResult.SyncSuccess ? HealthStatus.Healthy : HealthStatus.UnHealthy);

						Log.Information("Done");
						Log.Information("Sleeping for {@Seconds} seconds...", settings.App.PollingIntervalSeconds);

						var now = DateTime.UtcNow;
						var nextRunTime = now.AddSeconds(settings.App.PollingIntervalSeconds);
						NextSyncTime.Set(new DateTimeOffset(nextRunTime).ToUnixTimeSeconds());
						Thread.Sleep(settings.App.PollingIntervalSeconds * 1000);
					}
				} 
				else
				{
					await _syncService.SyncAsync(settings.Peloton.NumWorkoutsToDownload);
				}

				_logger.Information("Done.");
			}
			catch (Exception ex)
			{
				_logger.Fatal(ex, "Uncaught Exception");
				Health.Set(HealthStatus.Dead);
				exitCode = -2;
			}
			finally
			{
				_logger.Verbose("Exit.");

				if (!settings.App.CloseWindowOnFinish)
					Console.ReadLine();

				Environment.Exit(exitCode);
			}
		}
	}
}
