using Common;
using Common.Database;
using Common.Stateful;
using Conversion;
using Garmin;
using Microsoft.Extensions.Hosting;
using Peloton;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Common.Metrics;
using Metrics = Prometheus.Metrics;

namespace WebApp.Services
{
	public class BackgroundSyncJob : BackgroundService
	{
		private static readonly Histogram SyncHistogram = Metrics.CreateHistogram("p2g_sync_duration_seconds", "The histogram of sync jobs that have run.");
		private static readonly Gauge BuildInfo = Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Common.Metrics.Label.Version, Common.Metrics.Label.Os, Common.Metrics.Label.OsVersion, Common.Metrics.Label.DotNetRuntime }
		});
		private static readonly Gauge Health = Metrics.CreateGauge("p2g_sync_service_health", "Health status for P2G Sync Service.");
		private static readonly Gauge NextSyncTime = Metrics.CreateGauge("p2g_next_sync_time", "The next time the sync will run in seconds since epoch.");

		private static readonly ILogger _logger = LogContext.ForClass<BackgroundSyncJob>();

		private readonly IAppConfiguration _config;
		private readonly IPelotonService _pelotonService;
		private readonly IGarminUploader _garminUploader;
		private readonly IEnumerable<IConverter> _converters;
		private readonly IFileHandling _fileHandler;
		private readonly ISyncStatusDb _syncStatusDb;
		private bool? _previousPollingState;

		public BackgroundSyncJob(IAppConfiguration config, IPelotonService pelotonService, IGarminUploader garminUploader, IEnumerable<IConverter> converters, IFileHandling fileHandling, ISyncStatusDb syncStatusDb)
		{
			_config = config;
			_pelotonService = pelotonService;
			_garminUploader = garminUploader;
			_converters = converters;
			_fileHandler = fileHandling;
			_syncStatusDb = syncStatusDb;

			SyncServiceState.Enabled = _config.App.EnablePolling;
			SyncServiceState.PollingIntervalSeconds = _config.App.PollingIntervalSeconds;
			_previousPollingState = null;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			return RunAsync(stoppingToken);
		}

		private async Task RunAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				if (await NotPollingAsync())
					continue;

				await SyncAsync();

				_logger.Information("Sleeping for {@Seconds} seconds...", SyncServiceState.PollingIntervalSeconds);
				for (int i = 1; i < SyncServiceState.PollingIntervalSeconds; i++)
				{
					Thread.Sleep(1000);
					if (StateChanged()) break;
				}				
			}
		}

		private bool StateChanged()
		{
			return _previousPollingState != SyncServiceState.Enabled;
		}

		private async Task<bool> NotPollingAsync()
		{			
			var shouldPoll = SyncServiceState.Enabled;			

			if (StateChanged())
			{
				var syncTime = await _syncStatusDb.GetSyncStatusAsync();
				syncTime.NextSyncTime = shouldPoll ? DateTime.Now : null;
				syncTime.SyncStatus = shouldPoll ? Status.Running : Status.NotRunning;
				await _syncStatusDb.UpsertSyncStatusAsync(syncTime);

				if (shouldPoll) _logger.Information("Sync Service started.");
				else _logger.Information("Sync Service stopped.");
			}

			_previousPollingState = shouldPoll;
			return !shouldPoll;
		}

		private async Task SyncAsync()
		{
			try
			{
				using var timer = SyncHistogram.NewTimer();
				using var activity = Tracing.Trace(nameof(SyncAsync));				

				await _pelotonService.DownloadLatestWorkoutDataAsync();								

				foreach (var converter in _converters)
					converter.Convert();

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

			} catch (Exception e)
			{
				_logger.Error(e, "Uncaught Exception.");
			} finally
			{
				var now = DateTime.UtcNow;
				var nextRunTime = now.AddSeconds(_config.App.PollingIntervalSeconds);

				var syncStatus = await _syncStatusDb.GetSyncStatusAsync();
				syncStatus.LastSyncTime = DateTime.Now;
				syncStatus.LastSuccessfulSyncTime = Health.Value == HealthStatus.Healthy ? DateTime.Now : syncStatus.LastSuccessfulSyncTime;
				syncStatus.NextSyncTime = nextRunTime;
				syncStatus.SyncStatus = Health.Value == HealthStatus.UnHealthy ? Status.UnHealthy :
										Health.Value == HealthStatus.Dead ? Status.Dead :
										Status.Running;

				await _syncStatusDb.UpsertSyncStatusAsync(syncStatus);

				NextSyncTime.Set(new DateTimeOffset(nextRunTime).ToUnixTimeSeconds());
			}
		}
	} 
}
