using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Linq;
using PromMetrics = Prometheus.Metrics;

namespace Common.Database
{
	public interface IDbClient
	{
		SyncHistoryItem Get(string id);
		void Upsert(SyncHistoryItem item);
		SyncServiceStatus GetSyncTime();
		void UpsertSyncTime(SyncServiceStatus syncTime);
	}

	public class DbClient : IDbClient
	{
		private static readonly Histogram DbActionDuration = PromMetrics.CreateHistogram("p2g_db_duration_seconds", "Counter of db actions.", new HistogramConfiguration()
		{
			LabelNames = new[] { Metrics.Label.DbMethod, Metrics.Label.DbQuery }
		});
		private static readonly ILogger _logger = LogContext.ForClass<DbClient>();

		private DataStore _configDatabase;
		private Lazy<IDocumentCollection<SyncHistoryItem>> _syncHistoryTable;
		private IFileHandling _fileHandler;

		public DbClient(IAppConfiguration configuration, IFileHandling fileHandler)
		{
			_fileHandler = fileHandler;

			using var metrics = DbActionDuration
									.WithLabels("using", "syncHistoryTable")
									.NewTimer();
			using var tracing = Tracing.Trace("LoadTable", TagValue.Db).WithWorkoutId("SyncHistoryItem");

			if (!File.Exists(configuration.App.SyncHistoryDbPath))
			{
				_logger.Debug("Creating syncHistory db: {@Path}", configuration.App.SyncHistoryDbPath);
				try
				{
					var dir = Path.GetDirectoryName(configuration.App.SyncHistoryDbPath);
					_fileHandler.MkDirIfNotExists(dir);
					File.WriteAllText(configuration.App.SyncHistoryDbPath, "{}");

				} catch (Exception e)
				{
					_logger.Error(e, "Failed to create syncHistory db file: {@Path}", configuration.App.SyncHistoryDbPath);
					throw;
				}
			}

			_configDatabase = new DataStore(configuration.App.ConfigDbPath);
			var syncHistoryDb = new DataStore(configuration.App.SyncHistoryDbPath);
			_syncHistoryTable = new Lazy<IDocumentCollection<SyncHistoryItem>>(() => syncHistoryDb.GetCollection<SyncHistoryItem>());
		}

		public SyncHistoryItem Get(string id)
		{
			using var metrics = DbActionDuration
									.WithLabels("select", "workoutId")
									.NewTimer();
			using var tracing = Tracing.Trace("select", TagValue.Db)
										.WithTable("SyncHistoryItem")
										.WithWorkoutId(id);

			try
			{
				return _syncHistoryTable.Value.AsQueryable().Where(i => i.Id == id).FirstOrDefault();
			} 
			catch (Exception e)
			{
				_logger.Error(e, "Failed to get workout from db: {@WorkoutId}", id);
				return null;
			}
		}

		public SyncServiceStatus GetSyncTime()
		{
			using var metrics = DbActionDuration
									.WithLabels("select", "SyncTime")
									.NewTimer();
			using var tracing = Tracing.Trace("select", TagValue.Db)
										.WithTable("SyncTime");

			try
			{
				var syncTime = _configDatabase.GetItem<SyncServiceStatus>("syncTime");
				return syncTime ?? new SyncServiceStatus();

			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to get last sync time from db.");
				return new SyncServiceStatus();
			}
		}

		public void UpsertSyncTime(SyncServiceStatus newSyncTime)
		{
			using var metrics = DbActionDuration
									.WithLabels("upsert", "SyncTime")
									.NewTimer();
			using var tracing = Tracing.Trace("upsert", TagValue.Db)
										.WithTable("SyncTime");

			try
			{
				_configDatabase.ReplaceItem("syncTime", newSyncTime, upsert: true);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to upsert sync time in db.");
			}
		}

		public void Upsert(SyncHistoryItem item)
		{
			using var metrics = DbActionDuration
									.WithLabels("upsert", "workoutId")
									.NewTimer();
			using var tracing = Tracing.Trace("upsert", TagValue.Db)?
											.WithTable("SyncHistoryItem")
											.WithWorkoutId(item.Id);

			try
			{
				_syncHistoryTable.Value.ReplaceOne(item.Id, item, upsert: true);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to upsert workout to db: {@WorkoutId}", item?.Id);
			}
		}
	}
}
