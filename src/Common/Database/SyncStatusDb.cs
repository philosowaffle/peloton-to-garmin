using Common.Observe;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Database
{
    public interface ISyncStatusDb
    {
        Task<SyncServiceStatus> GetSyncStatusAsync();
        Task UpsertSyncStatusAsync(SyncServiceStatus status);
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

        public Task<SyncServiceStatus> GetSyncStatusAsync()
        {
            using var metrics = DbMetrics.DbActionDuration
                                    .WithLabels("get", DbName)
                                    .NewTimer();
            using var tracing = Tracing.Trace($"{nameof(SyncStatusDb)}.{nameof(GetSyncStatusAsync)}", TagValue.Db)
                                        .WithTable(DbName);

            try
            {
                return Task.FromResult(_db.GetItem<SyncServiceStatus>("syncServiceStatus"));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get syncServiceStatus from db");
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
                return _db.ReplaceItemAsync("syncServiceStatus", status, upsert: true);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to upsert syncServiceStatus to db");
                return Task.FromResult(false);
            }
        }
    }
}
