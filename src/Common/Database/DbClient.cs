using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Linq;
using PromMetrics = Prometheus.Metrics;

namespace Common.Database
{
	public class DbClient
	{
		private static readonly Histogram DbActionDuration = PromMetrics.CreateHistogram("p2g_db_action_duration_seconds", "Histogram of db action durations.", new HistogramConfiguration()
		{
			LabelNames = new[] { "action", "queryName" }
		});

		private DataStore _database;
		private Lazy<IDocumentCollection<SyncHistoryItem>> _syncHistoryTable;

		public DbClient(Configuration configuration)
		{
			using var metrics = DbActionDuration
									.WithLabels("using", "syncHistoryTable")
									.NewTimer();
			using var tracing = Tracing.Trace("LoadTable", TagValue.Db).WithWorkoutId("SyncHistoryItem");

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
