﻿using Common;
using Common.Database;
using Common.Observe;
using Common.Service;
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
using static Common.Observe.Metrics;
using Metrics = Common.Observe.Metrics;
using PromMetrics = Prometheus.Metrics;

namespace WebApp.Services
{
    public class BackgroundSyncJob : BackgroundService
	{
		private static readonly Histogram SyncHistogram = PromMetrics.CreateHistogram("p2g_sync_duration_seconds", "The histogram of sync jobs that have run.");
		private static readonly Gauge BuildInfo = PromMetrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Metrics.Label.Version, Metrics.Label.Os, Metrics.Label.OsVersion, Metrics.Label.DotNetRuntime }
		});
		private static readonly Gauge Health = PromMetrics.CreateGauge("p2g_sync_service_health", "Health status for P2G Sync Service.");
		private static readonly Gauge NextSyncTime = PromMetrics.CreateGauge("p2g_next_sync_time", "The next time the sync will run in seconds since epoch.");

		private static readonly ILogger _logger = LogContext.ForClass<BackgroundSyncJob>();

		private readonly ISettingsService _settingsService;
		private readonly IPelotonService _pelotonService;
		private readonly IGarminUploader _garminUploader;
		private readonly IEnumerable<IConverter> _converters;
		private readonly IFileHandling _fileHandler;
		private readonly ISyncStatusDb _syncStatusDb;
		
		private bool? _previousPollingState;
		private Settings _config;


		public BackgroundSyncJob(ISettingsService settingsService, IPelotonService pelotonService, IGarminUploader garminUploader, IEnumerable<IConverter> converters, IFileHandling fileHandling, ISyncStatusDb syncStatusDb)
		{
			_settingsService = settingsService;
			_pelotonService = pelotonService;
			_garminUploader = garminUploader;
			_converters = converters;
			_fileHandler = fileHandling;
			_syncStatusDb = syncStatusDb;

			_previousPollingState = null;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			return RunAsync(stoppingToken);
		}

		private async Task RunAsync(CancellationToken stoppingToken)
		{
			_config = await _settingsService.GetSettingsAsync();
			SyncServiceState.Enabled = _config.App.EnablePolling;
			SyncServiceState.PollingIntervalSeconds = _config.App.PollingIntervalSeconds;

			while (!stoppingToken.IsCancellationRequested)
			{
				int stepIntervalSeconds = 5;

				if (await NotPollingAsync())
                {
					Thread.Sleep(stepIntervalSeconds * 1000);
					continue;
				}					

				await SyncAsync();

				_logger.Information("Sleeping for {@Seconds} seconds...", SyncServiceState.PollingIntervalSeconds);
				
				for (int i = 1; i < SyncServiceState.PollingIntervalSeconds; i+=stepIntervalSeconds)
				{
					Thread.Sleep(stepIntervalSeconds * 1000);
					if (await StateChangedAsync()) break;
				}				
			}
		}

		private async Task<bool> StateChangedAsync()
		{
            using var tracing = Tracing.Trace($"{nameof(BackgroundService)}.{nameof(StateChangedAsync)}");

            _config = await _settingsService.GetSettingsAsync();
			SyncServiceState.Enabled = _config.App.EnablePolling;
			SyncServiceState.PollingIntervalSeconds = _config.App.PollingIntervalSeconds;

			return _previousPollingState != SyncServiceState.Enabled;
		}

		private async Task<bool> NotPollingAsync()
		{
            using var tracing = Tracing.Trace($"{nameof(BackgroundService)}.{nameof(NotPollingAsync)}");

            if (await StateChangedAsync())
			{
				var syncTime = await _syncStatusDb.GetSyncStatusAsync();
				syncTime.NextSyncTime = SyncServiceState.Enabled ? DateTime.Now : null;
				syncTime.SyncStatus = SyncServiceState.Enabled ? Status.Running : Status.NotRunning;
				await _syncStatusDb.UpsertSyncStatusAsync(syncTime);

				if (SyncServiceState.Enabled) _logger.Information("Sync Service started.");
				else _logger.Information("Sync Service stopped.");
			}

			_previousPollingState = SyncServiceState.Enabled;
			return !SyncServiceState.Enabled;
		}

		private async Task SyncAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(BackgroundService)}.{nameof(SyncAsync)}");

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
