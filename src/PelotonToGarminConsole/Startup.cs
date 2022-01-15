using Common;
using Common.Observe;
using Microsoft.Extensions.Hosting;
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

        public Startup(IAppConfiguration configuration, ISyncService service)
        {
            _config = configuration;

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

            return RunAsync(cancelToken);
        }

        private Task RunAsync(CancellationToken cancelToken)
        {
            using var metrics = Metrics.EnableMetricsServer(_config.Observability.Prometheus);
            using var metricsCollector = Metrics.EnableCollector(_config.Observability.Prometheus);
            using var tracing = Tracing.EnableTracing(_config.Observability.Jaeger);
            using var tracingSource = new ActivitySource("ROOT");

            try
            {
                _dockerClient.BeginEventMonitoringAsync();

                while (!cancelToken.IsCancellationRequested) { }

                return Task.CompletedTask;

            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "RunAsync failed.");
                Health.Set(HealthStatus.Dead);
                return Task.CompletedTask;

            }
            finally
            {
                _logger.Verbose("End.");
            }
        }
    }
}
