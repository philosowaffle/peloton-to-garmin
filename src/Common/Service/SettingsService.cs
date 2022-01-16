﻿using Common.Database;
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

        public async Task UpdateSettings(Settings updatedSettings)
        {
            var originalSettings = await _db.GetSettingsAsync();

            if (updatedSettings.Garmin.Email is null)
                updatedSettings.Garmin.Email = originalSettings.Garmin.Email;

            if (updatedSettings.Garmin.Password is null)
                updatedSettings.Garmin.Password = originalSettings.Garmin.Password;

            if (updatedSettings.Peloton.Email is null)
                updatedSettings.Peloton.Email = originalSettings.Peloton.Email;

            if (updatedSettings.Peloton.Password is null)
                updatedSettings.Peloton.Password = originalSettings.Peloton.Password;

            await _db.UpsertSettingsAsync(updatedSettings);
        }
    }
}
