using Common.Helpers;
using Common.Observe;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Database
{
	public interface ISettingsDb
	{
		[Obsolete]
		Settings GetLegacySettings();
		[Obsolete]
		Task RemoveLegacySettingsAsync();
		Task<Settings> GetSettingsAsync(int userId);
		Task<bool> UpsertSettingsAsync(int userId, Settings settings);
	}

	public class SettingsDb : DbBase<Settings>, ISettingsDb
	{
		private static readonly ILogger _logger = LogContext.ForClass<SettingsDb>();

		private readonly DataStore _db;
		private readonly Settings _defaultSettings = new Settings();

		public SettingsDb(IFileHandling fileHandler) : base("Settings", fileHandler)
		{
			_db = new DataStore(DbPath);
		}

		public Settings GetLegacySettings()
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(GetLegacySettings)}", TagValue.Db)
										.WithTable(DbName);

			try
			{
				var settings = _db.GetItem<Settings>("settings");
				settings.Peloton.Decrypt();
				settings.Garmin.Decrypt();

				return settings;
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		public async Task<Settings> GetSettingsAsync(int userId)
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(GetSettingsAsync)}.ByUserId", TagValue.Db)
										.WithTable(DbName);

			try
			{
				var settings = _db.GetItem<Settings>(userId.ToString());
				settings.Peloton.Decrypt();
				settings.Garmin.Decrypt();

				return settings;
			}
			catch (KeyNotFoundException k)
			{
				_logger.Verbose(k, $"Settings key not found in DB for user {userId}. Creating default Settings.");

				var success = await _db.InsertItemAsync(userId.ToString(), _defaultSettings);
				if (!success)
				{
					_logger.Error($"Failed to save default Settings to Db for user {userId}");
					throw;
				}

				return _defaultSettings;
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Failed to upsert settings to db for user {userId}");
				throw;
			}
		}

		public Task<bool> UpsertSettingsAsync(int userId, Settings settings)
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsert", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(UpsertSettingsAsync)}", TagValue.Db)
										.WithTable(DbName);

			try
			{
				settings.Peloton.Encrypt();
				settings.Garmin.Encrypt();

				return _db.ReplaceItemAsync(userId.ToString(), settings, upsert: true);
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Failed to upsert settings to db for user {userId}");
				return Task.FromResult(false);
			}
		}

		public Task RemoveLegacySettingsAsync()
		{
			return _db.DeleteItemAsync("settings");
		}
	}
}
