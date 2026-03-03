using Common;
using Common.Database;
using Common.Observe;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using Sync.Dto;
using System;
using System.Threading.Tasks;

namespace Sync.Database
{
	public interface ISyncStatusDb
	{
		Task<SyncServiceStatus> GetSyncStatusAsync();
		Task UpsertSyncStatusAsync(SyncServiceStatus status);
		Task DeleteLegacySyncStatusAsync();
	}

	public class SyncStatusDb : DbBase<SyncServiceStatus>, ISyncStatusDb
	{
		private static readonly ILogger _logger = LogContext.ForClass<SyncStatusDb>();

		private readonly DataStore _db;

		public SyncStatusDb(IFileHandling fileHandling) : base("SyncStatus", fileHandling)
		{
			_db = new DataStore(DbPath);
			Init();
		}

		private void Init()
		{
			try
			{
				var settings = _db.GetItem<SyncServiceStatus>("1");

				if (_db.TryGetItem<SyncServiceStatus>(1, out var syncStatus))
					return;

				if (_db.InsertItem("1", new SyncServiceStatus()))
					return;
			}
			catch (Exception e)
			{
				_logger.Error($"Failed to init default Sync Status to Db for default user.", e);
			}
		}

		public Task DeleteLegacySyncStatusAsync()
		{
			return _db.DeleteItemAsync("syncServiceStatus");
		}

		public async Task<SyncServiceStatus> GetSyncStatusAsync()
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SyncStatusDb)}.{nameof(GetSyncStatusAsync)}", TagValue.Db)
										.WithTable(DbName);

			try
			{
				if (_db.TryGetItem<SyncServiceStatus>(1, out var syncStatus))
					return syncStatus;

				if (await _db.InsertItemAsync("1", new SyncServiceStatus()))
					return new SyncServiceStatus();

				_logger.Error("Failed to save default SyncServiceStatus to Sync DB.");
				return new SyncServiceStatus();
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to get syncServiceStatus from db for user 1");
				return new SyncServiceStatus();
			}
		}

		public Task UpsertSyncStatusAsync(SyncServiceStatus status)
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsert", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SyncStatusDb)}.{nameof(UpsertSyncStatusAsync)}", TagValue.Db)
										.WithTable(DbName);

			try
			{
				return _db.ReplaceItemAsync("1", status, upsert: true); // hardcode to admin for now
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to upsert syncServiceStatus to db for user 1");
				return Task.FromResult(false);
			}
		}
	}
}
