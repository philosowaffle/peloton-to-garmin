using Common.Database;
using Serilog;
using System.Threading.Tasks;

namespace Common.Service
{
    public interface ISettingsService
    {
        AppConfiguration GetAppConfiguration();
        Task<Settings> GetSettingsAsync();
        Task UpdateSettings(Settings settings);
    }

    public class SettingsService : ISettingsService
    {
        private static readonly ILogger _logger = LogContext.ForClass<SettingsService>();

        private readonly ISettingsDb _db;
        private readonly IAppConfiguration _appConfiguration;

        public SettingsService(ISettingsDb db, IAppConfiguration appConfiguration)
        {
            _db = db;
            _appConfiguration = appConfiguration;
        }

        public AppConfiguration GetAppConfiguration()
        {
            var appConfig = _appConfiguration;
            return new AppConfiguration()
            {
                Observability = appConfig.Observability,
                Developer = appConfig.Developer
            };
        }

        public Task<Settings> GetSettingsAsync()
        {
            return _db.GetSettingsAsync();
        }

        public Task UpdateSettings(Settings settings)
        {
            return _db.UpsertSettingsAsync(settings);
        }
    }
}
