using Common.Observe;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Database
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
		private readonly SyncServiceStatus _defaultSyncServiceStatus = new SyncServiceStatus();

		public SyncStatusDb(IFileHandling fileHandling) : base("SyncStatus", fileHandling)
		{
			_db = new DataStore(DbPath);
		}

		public Task DeleteLegacySyncStatusAsync()
		{
			return _db.DeleteItemAsync("syncServiceStatus");
		}

		public Task<SyncServiceStatus> GetSyncStatusAsync()
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SyncStatusDb)}.{nameof(GetSyncStatusAsync)}", TagValue.Db)
										.WithTable(DbName);

			try
			{
				return Task.FromResult(_db.GetItem<SyncServiceStatus>("1")); // hardcode to admin for now
			}
			catch(KeyNotFoundException k)
			{
				_logger.Verbose("syncServiceStatus key not found in DB for user 1.", k);
				return Task.FromResult(new SyncServiceStatus());
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to get syncServiceStatus from db for user 1");
				return Task.FromResult(_defaultSyncServiceStatus);
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
