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
	}

	public class DbClient : IDbClient
	{
		private static readonly Histogram DbActionDuration = PromMetrics.CreateHistogram("p2g_db_duration_seconds", "Counter of db actions.", new HistogramConfiguration()
		{
			LabelNames = new[] { Metrics.Label.DbMethod, Metrics.Label.DbQuery }
		});

		private DataStore _database;
		private Lazy<IDocumentCollection<SyncHistoryItem>> _syncHistoryTable;
		private IFileHandling _fileHandler;

		public DbClient(Configuration configuration, IFileHandling fileHandler)
		{
			_fileHandler = fileHandler;

			using var metrics = DbActionDuration
									.WithLabels("using", "syncHistoryTable")
									.NewTimer();
			using var tracing = Tracing.Trace("LoadTable", TagValue.Db).WithWorkoutId("SyncHistoryItem");

			if (!File.Exists(configuration.App.SyncHistoryDbPath))
			{
				Log.Debug("Creating syncHistory db: {@Path}", configuration.App.SyncHistoryDbPath);
				try
				{
					var dir = Path.GetDirectoryName(configuration.App.SyncHistoryDbPath);
					_fileHandler.MkDirIfNotExists(dir);
					File.WriteAllText(configuration.App.SyncHistoryDbPath, "{}");

				} catch (Exception e)
				{
					Log.Error(e, "Failed to create syncHistory db file: {@Path}", configuration.App.SyncHistoryDbPath);
					throw;
				}
			}

			_database = new DataStore(configuration.App.SyncHistoryDbPath);
			_syncHistoryTable = new Lazy<IDocumentCollection<SyncHistoryItem>>(() => _database.GetCollection<SyncHistoryItem>());
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
				Log.Error(e, "Failed to get workout from db: {@WorkoutId}", id);
				return null;
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
				Log.Error(e, "Failed to upsert workout to db: {@WorkoutId}", item?.Id);
			}
		}
	}
}
