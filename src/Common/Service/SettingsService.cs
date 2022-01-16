using Common.Database;
using Common.Observe;
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

        public async Task<Settings> GetSettingsAsync()
        {
            using var tracing = Tracing.Trace(nameof(GetSettingsAsync));

            var settings = await _db.GetSettingsAsync();

            if (settings is null)
            {
                return new Settings()
                {
                    App = _appConfiguration.App,
                    Format = _appConfiguration.Format,
                    Garmin = _appConfiguration.Garmin,
                    Peloton = _appConfiguration.Peloton,
                };
            }

            return settings;
        }

        public Task UpdateSettings(Settings settings)
        {
            return _db.UpsertSettingsAsync(settings);
        }
    }
}
