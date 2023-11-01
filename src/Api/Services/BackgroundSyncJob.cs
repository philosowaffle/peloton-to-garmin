using Common;
using Common.Database;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Prometheus;
using Sync;
using static Common.Observe.Metrics;
using ILogger = Serilog.ILogger;
using PromMetrics = Prometheus.Metrics;

namespace Api.Services;

public class BackgroundSyncJob : BackgroundService
{
	private static readonly Histogram SyncHistogram = PromMetrics.CreateHistogram("p2g_sync_duration_seconds", "The histogram of sync jobs that have run.");
	private static readonly Gauge Health = PromMetrics.CreateGauge("p2g_sync_service_health", "Health status for P2G Sync Service.");
	private static readonly Gauge NextSyncTime = PromMetrics.CreateGauge("p2g_next_sync_time", "The next time the sync will run in seconds since epoch.");

	private static readonly ILogger _logger = LogContext.ForClass<BackgroundSyncJob>();

	private readonly ISettingsService _settingsService;
	private readonly ISyncStatusDb _syncStatusDb;
	private readonly ISyncService _syncService;
		
	private bool? _previousPollingState;
	private Settings _config;


	public BackgroundSyncJob(ISettingsService settingsService,ISyncStatusDb syncStatusDb, ISyncService syncService)
	{
		_settingsService = settingsService;
		_syncStatusDb = syncStatusDb;

		_previousPollingState = null;
		_syncService = syncService;

		_config = new Settings();
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Health.Set(HealthStatus.Healthy);
		return RunAsync(stoppingToken);
	}

	private async Task RunAsync(CancellationToken stoppingToken)
	{
		_config = await _settingsService.GetSettingsAsync();

		if (_config.Garmin.Upload && _config.Garmin.TwoStepVerificationEnabled && _config.App.EnablePolling)
		{
			_logger.Error("Background Sync cannot be enabled when Garmin TwoStepVerification is enabled.");
			_logger.Information("Sync Service stopped.");
			return;
		}

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
			var result = await _syncService.SyncAsync(_config.Peloton.NumWorkoutsToDownload);
			if(result.SyncSuccess)
			{
				Health.Set(HealthStatus.Healthy);
			} else
			{
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
			syncStatus.NextSyncTime = nextRunTime;
			syncStatus.SyncStatus = Health.Value == HealthStatus.UnHealthy ? Status.UnHealthy :
									Status.Running;

			await _syncStatusDb.UpsertSyncStatusAsync(syncStatus);

			NextSyncTime.Set(new DateTimeOffset(nextRunTime).ToUnixTimeSeconds());
		}
	}
}
