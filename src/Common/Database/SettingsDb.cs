using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Database
{
    public interface ISettingsDb
    {
        Task<Settings> GetSettingsAsync();
        Task UpsertSettingsAsync(Settings settings);
    }

    public class SettingsDb : DbBase<Settings>, ISettingsDb
    {
        private static readonly ILogger _logger = LogContext.ForClass<SettingsDb>();

        private readonly DataStore _db;
        private readonly Settings _defaultSettings = new Settings();

        public SettingsDb(IFileHandling fileHandler) : base("Settings", "/data/settingsDb.json", fileHandler)
        {
            _db = new DataStore(DbPath);
        }

        public Task<Settings> GetSettingsAsync()
        {
            using var metrics = DbMetrics.DbActionDuration
                                    .WithLabels("get", DbName)
                                    .NewTimer();
            using var tracing = Tracing.Trace("get", TagValue.Db)
                                        .WithTable(DbName);

            try
            {
                return Task.FromResult(_db.GetItem<Settings>("settings"));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to get settings from db");
                return Task.FromResult(_defaultSettings);
            }
        }

        public Task UpsertSettingsAsync(Settings settings)
        {
            using var metrics = DbMetrics.DbActionDuration
                                    .WithLabels("upsert", DbName)
                                    .NewTimer();
            using var tracing = Tracing.Trace("upsert", TagValue.Db)
                                        .WithTable(DbName);

            try
            {
                return _db.ReplaceItemAsync("settings", settings, upsert: true);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to upsert settings to db");
                return Task.FromResult(false);
            }
        }
    }
}
