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
		Task<Settings> GetSettingsAsync();
		Task UpsertSettingsAsync(Settings settings);
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

		public async Task<Settings> GetSettingsAsync()
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(GetSettingsAsync)}", TagValue.Db)
										.WithTable(DbName);

			try
			{
				return _db.GetItem<Settings>("settings");
			}
			catch (KeyNotFoundException k)
			{
				_logger.Verbose(k, "Settings key not found in DB. Creating default Settings.");

				var success = await _db.InsertItemAsync("settings", _defaultSettings);
				if (!success)
				{
					_logger.Error("Failed to save default Settings to Db.");
					throw;
				}
	
				return _defaultSettings;
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to get settings from db");
				throw;
			}
		}

		public Task UpsertSettingsAsync(Settings settings)
		{
			using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsert", DbName)
									.NewTimer();
			using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(UpsertSettingsAsync)}", TagValue.Db)
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
