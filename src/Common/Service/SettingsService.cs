using Common.Database;
using Common.Observe;
using Serilog;
using System.Threading.Tasks;

namespace Common.Service
{
    public interface ISettingsService
    {
        Task<Settings> GetSettingsAsync();
        Task UpdateSettings(Settings settings);
    }

    public class SettingsService : ISettingsService
    {
        private static readonly ILogger _logger = LogContext.ForClass<SettingsService>();

        private readonly ISettingsDb _db;

        public SettingsService(ISettingsDb db)
        {
            _db = db;
        }

        public Task<Settings> GetSettingsAsync()
        {
            using var tracing = Tracing.Trace(nameof(GetSettingsAsync));

            return _db.GetSettingsAsync();
        }

        public Task UpdateSettings(Settings settings)
        {
            return _db.UpsertSettingsAsync(settings);
        }
    }
}
