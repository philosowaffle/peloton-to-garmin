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

        private readonly IAppConfiguration _config;
        private readonly ISyncService _syncService;

        public Startup(IAppConfiguration configuration, ISyncService syncService)
        {
            _config = configuration;
            _syncService = syncService;

            FlurlConfiguration.Configure(_config);

            var runtimeVersion = Environment.Version.ToString();
            var os = Environment.OSVersion.Platform.ToString();
            var osVersion = Environment.OSVersion.VersionString;
            var assembly = Assembly.GetExecutingAssembly();
            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = versionInfo.ProductVersion ?? "unknown";

            BuildInfo.WithLabels(version, os, osVersion, runtimeVersion).Set(1);
            Log.Debug("App Version: {@Version}", version);
            Log.Debug("Operating System: {@Os}", osVersion);
            Log.Debug("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);
        }

        protected override Task ExecuteAsync(CancellationToken cancelToken)
        {
            _logger.Verbose("Begin.");

            Health.Set(HealthStatus.Healthy);

            try
            {
                PelotonService.ValidateConfig(_config.Peloton);
                GarminUploader.ValidateConfig(_config);
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
            using var metrics = Metrics.EnableMetricsServer(_config.Observability.Prometheus);
            using var metricsCollector = Metrics.EnableCollector(_config.Observability.Prometheus);
            using var tracing = Tracing.EnableTracing(_config.Observability.Jaeger);
            using var tracingSource = new ActivitySource("ROOT");

            int exitCode = 0;

            try
            {
                if (_config.Peloton.NumWorkoutsToDownload <= 0)
                {
                    Console.Write("How many workouts to grab? ");
                    int num = Convert.ToInt32(Console.ReadLine());
                    _config.Peloton.NumWorkoutsToDownload = num;
                }

                if (_config.App.EnablePolling)
                {
                    while (_config.App.EnablePolling && !cancelToken.IsCancellationRequested)
                    {
                        var syncResult = await _syncService.SyncAsync(_config.Peloton.NumWorkoutsToDownload);
                        Health.Set(syncResult.SyncSuccess ? HealthStatus.Healthy : HealthStatus.UnHealthy);

                        Log.Information("Sleeping for {@Seconds} seconds...", _config.App.PollingIntervalSeconds);

                        var now = DateTime.UtcNow;
                        var nextRunTime = now.AddSeconds(_config.App.PollingIntervalSeconds);
                        NextSyncTime.Set(new DateTimeOffset(nextRunTime).ToUnixTimeSeconds());
                        Thread.Sleep(_config.App.PollingIntervalSeconds * 1000);
                    }
                } 
                else
                {
                    await _syncService.SyncAsync(_config.Peloton.NumWorkoutsToDownload);
                }

                Log.Information("Done.");
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

                if (!_config.App.CloseWindowOnFinish)
                    Console.ReadLine();

                Environment.Exit(exitCode);
            }
        }
    }
}
