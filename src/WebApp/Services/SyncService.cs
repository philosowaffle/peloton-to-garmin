using Common;
using Common.Database;
using Conversion;
using Garmin;
using Microsoft.Extensions.Hosting;
using Peloton;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Common.Metrics;
using Metrics = Prometheus.Metrics;

namespace WebApp.Services
{
	public class SyncService : BackgroundService
	{
		private static readonly Histogram SyncHistogram = Metrics.CreateHistogram("p2g_sync_duration_seconds", "The histogram of sync jobs that have run.");
		private static readonly Gauge BuildInfo = Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Common.Metrics.Label.Version, Common.Metrics.Label.Os, Common.Metrics.Label.OsVersion, Common.Metrics.Label.DotNetRuntime }
		});
		private static readonly Gauge Health = Metrics.CreateGauge("p2g_sync_service_health", "Health status for P2G Sync Service.");
		private static readonly Gauge NextSyncTime = Metrics.CreateGauge("p2g_next_sync_time", "The next time the sync will run in seconds since epoch.");

		private static readonly ILogger _logger = LogContext.ForClass<SyncService>();

		private readonly IAppConfiguration _config;
		private readonly IPelotonService _pelotonService;
		private readonly IGarminUploader _garminUploader;
		private readonly IConverter _converter;
		private readonly IFileHandling _fileHandler;
		private readonly IDbClient _db;

		public SyncService(IAppConfiguration config, IPelotonService pelotonService, IGarminUploader garminUploader, IConverter converter, IFileHandling fileHandling, IDbClient db)
		{
			_config = config;
			_pelotonService = pelotonService;
			_garminUploader = garminUploader;
			_converter = converter;
			_fileHandler = fileHandling;
			_db = db;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			if (!_config.App.EnablePolling)
			{
				var syncTime = _db.GetSyncTime();
				syncTime.NextSyncTime = null;
				syncTime.AutoSyncServiceStatus = "Not Running";
				_db.UpsertSyncTime(syncTime);

				return Task.CompletedTask;

			}
				

			_logger.Information("Starting Sync Service....");
			return RunAsync();
		}

		private async Task RunAsync()
		{
			try
			{
				_logger.Information("Sync Service started.");
				while (_config.App.EnablePolling)
				{
					using var timer = SyncHistogram.NewTimer();
					using var activity = Tracing.Trace(nameof(RunAsync));

					var syncTime = _db.GetSyncTime();
					syncTime = syncTime ?? new SyncTime();
					syncTime.LastSyncTime = DateTime.Now;

					await _pelotonService.DownloadLatestWorkoutDataAsync();

					_converter.Convert();

					//var tcxConverter = new TcxConverter(config, db, fileHandler);
					//tcxConverter.Convert();

					try
					{
						await _garminUploader.UploadToGarminAsync();
						Health.Set(HealthStatus.Healthy);

						_fileHandler.Cleanup(_config.App.DownloadDirectory);
						_fileHandler.Cleanup(_config.App.UploadDirectory);
						foreach (var file in Directory.GetFiles(_config.App.WorkingDirectory))
							File.Delete(file);
					}
					catch (GarminUploadException e)
					{
						_logger.Error(e, "Garmin upload returned an error code. Failed to upload workouts.");
						_logger.Warning("GUpload failed to upload files. You can find the converted files at {@Path} \n You can manually upload your files to Garmin Connect, or wait for P2G to try again on the next sync job.", _config.App.OutputDirectory);
						Health.Set(HealthStatus.UnHealthy);
					}

					_logger.Information("Sleeping for {@Seconds} seconds...", _config.App.PollingIntervalSeconds);

					var now = DateTime.UtcNow;
					var nextRunTime = now.AddSeconds(_config.App.PollingIntervalSeconds);
					
					syncTime.NextSyncTime = nextRunTime;
					syncTime.LastSuccessfulSyncTime = Health.Value == HealthStatus.Healthy ? DateTime.Now : syncTime.LastSuccessfulSyncTime;
					syncTime.AutoSyncServiceStatus = Health.Value == HealthStatus.Healthy ? "Healthy" : "Uhealthy";
					_db.UpsertSyncTime(syncTime);

					NextSyncTime.Set(new DateTimeOffset(nextRunTime).ToUnixTimeSeconds());
					Thread.Sleep(_config.App.PollingIntervalSeconds * 1000);
				}
			} catch (Exception e)
			{
				_logger.Fatal(e, "Uncaught Exception.");
				_logger.Information("Sync Service no longer running.");

				Health.Set(HealthStatus.Dead);
				var syncTime = _db.GetSyncTime();
				syncTime.NextSyncTime = null;
				syncTime.AutoSyncServiceStatus = "Dead";
				_db.UpsertSyncTime(syncTime);
			}
		}
	} 
}
