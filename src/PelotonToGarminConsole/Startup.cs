using Common;
using Common.Observe;
using Garmin;
using Microsoft.Extensions.Hosting;
using Peloton;
using Prometheus;
using Serilog;
using Sync;
using System;
using System.Diagnostics;
using System.Reflection;
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

        private readonly AppConfiguration _config;
        private readonly Settings _settings;
        private readonly ISyncService _syncService;

        public Startup(AppConfiguration configuration, Settings settings, ISyncService syncService)
        {
            _config = configuration;
            _settings = settings;
            _syncService = syncService;

            FlurlConfiguration.Configure(_config.Observability);

            var runtimeVersion = Environment.Version.ToString();
            var os = Environment.OSVersion.Platform.ToString();
            var osVersion = Environment.OSVersion.VersionString;
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = versionInfo.ProductVersion ?? "unknown";

            BuildInfo.WithLabels(version, os, osVersion, runtimeVersion).Set(1);
            _logger.Debug("App Version: {@Version}", version);
            _logger.Debug("Operating System: {@Os}", osVersion);
            _logger.Debug("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);
        }

        protected override Task ExecuteAsync(CancellationToken cancelToken)
        {
            _logger.Verbose("Begin.");

            Health.Set(HealthStatus.Healthy);

            try
            {
                PelotonService.ValidateConfig(_settings.Peloton);
                GarminUploader.ValidateConfig(_settings);
                Metrics.ValidateConfig(_config.Observability);
                Tracing.ValidateConfig(_config.Observability);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Exception during config validation.");
                Health.Set(HealthStatus.Dead);
                Environment.Exit(-1);
            }            

            return RunAsync(cancelToken);
        }

        private async Task RunAsync(CancellationToken cancelToken)
        {
            int exitCode = 0;

            using var metrics = Metrics.EnableMetricsServer(_config.Observability.Prometheus);
            using var metricsCollector = Metrics.EnableCollector(_config.Observability.Prometheus);
            using var tracing = Tracing.EnableTracing(_config.Observability.Jaeger);
            using var tracingSource = new ActivitySource("ROOT");

            try
            {
                if (_settings.Peloton.NumWorkoutsToDownload <= 0)
                {
                    Console.Write("How many workouts to grab? ");
                    int num = Convert.ToInt32(Console.ReadLine());
                    _settings.Peloton.NumWorkoutsToDownload = num;
                }

                if (_settings.App.EnablePolling)
                {
                    while (_settings.App.EnablePolling && !cancelToken.IsCancellationRequested)
                    {
                        var syncResult = await _syncService.SyncAsync(_settings.Peloton.NumWorkoutsToDownload);
                        Health.Set(syncResult.SyncSuccess ? HealthStatus.Healthy : HealthStatus.UnHealthy);

                        Log.Information("Sleeping for {@Seconds} seconds...", _settings.App.PollingIntervalSeconds);

                        var now = DateTime.UtcNow;
                        var nextRunTime = now.AddSeconds(_settings.App.PollingIntervalSeconds);
                        NextSyncTime.Set(new DateTimeOffset(nextRunTime).ToUnixTimeSeconds());
                        Thread.Sleep(_settings.App.PollingIntervalSeconds * 1000);
                    }
                } 
                else
                {
                    await _syncService.SyncAsync(_settings.Peloton.NumWorkoutsToDownload);
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

                if (!_settings.App.CloseWindowOnFinish)
                    Console.ReadLine();

                Environment.Exit(exitCode);
            }
        }
    }
}
