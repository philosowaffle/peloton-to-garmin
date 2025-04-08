using Common.Dto;
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
		Settings DbMigrations_TryGetLegacy_Settings();
		[Obsolete]
		Task DbMigrations_TryRemoveLegacySettingsAsync();
		[Obsolete]
		Task<DbMigrations_LegacyDeviceInfo> DbMigrations_TryGetLegacy_DeviceInfoSettings(int userId);
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
			Init();
		}

		private void Init()
		{
			try
			{
				var settings = _db.GetItem<Settings>("1");
			}
			catch (KeyNotFoundException)
			{
				var success = _db.InsertItem("1", _defaultSettings);
				if (!success)
				{
					_logger.Error($"Failed to init default Settings to Db for default user.");
				}
			}
		}

		public Settings DbMigrations_TryGetLegacy_Settings()
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(DbMigrations_TryGetLegacy_Settings)}", TagValue.Db)
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

		public Task<DbMigrations_LegacyDeviceInfo> DbMigrations_TryGetLegacy_DeviceInfoSettings(int userId)
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(DbMigrations_TryGetLegacy_DeviceInfoSettings)}.ByUserId", TagValue.Db)
										.WithTable(DbName);

			try
			{
				var settings = _db.GetItem<DbMigrations_LegacyDeviceInfo>(userId.ToString());

				return Task.FromResult(settings);
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Failed to get settings to db for user {userId}");
				throw;
			}
		}

		public Task<Settings> GetSettingsAsync(int userId)
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

				return Task.FromResult(settings);
			}
			catch (Exception e)
			{
				_logger.Error(e, $"Failed to get settings to db for user {userId}");
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

		public Task DbMigrations_TryRemoveLegacySettingsAsync()
		{
			return _db.DeleteItemAsync("settings");
		}
	}
}
