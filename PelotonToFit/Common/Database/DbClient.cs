using JsonFlatFileDataStore;
using Prometheus;
using System;
using System.Linq;

namespace Common.Database
{
	public class DbClient
	{
		private DataStore _database;
		private Lazy<IDocumentCollection<SyncHistoryItem>> _syncHistoryTable;

		public DbClient(Configuration configuration)
		{
			using var metrics = Metrics.DbActionDuration
										.WithLabels("using", "syncHistoryTable")
										.NewTimer();
			using var tracing = Tracing.Source?.StartActivity("LoadTable")?
										.SetTag(Tracing.Table, "SyncHistoryItem")?
										.SetTag(Tracing.Category, Tracing.Db);

				_database = new DataStore(configuration.App.SyncHistoryDbPath);
				_syncHistoryTable = new Lazy<IDocumentCollection<SyncHistoryItem>>(() => _database.GetCollection<SyncHistoryItem>());
		}

		public SyncHistoryItem Get(string id)
		{
			using var metrics = Metrics.DbActionDuration
										.WithLabels("select", "workoutId")
										.NewTimer();
			using var tracing = Tracing.Source?.StartActivity("select")?
											.SetTag(Tracing.Table, "SyncHistoryItem")?
											.SetTag(Tracing.WorkoutId, id)?
											.SetTag(Tracing.Category, Tracing.Db);

				return _syncHistoryTable.Value.AsQueryable().Where(i => i.Id == id).FirstOrDefault();
		}

		public void Upsert(SyncHistoryItem item)
		{
			using var metrics = Metrics.DbActionDuration
										.WithLabels("upsert", "workoutId")
										.NewTimer();
			using var tracing = Tracing.Source?.StartActivity("upsert")?
											.SetTag(Tracing.Table, "SyncHistoryItem")?
											.SetTag(Tracing.WorkoutId, item.Id)?
											.SetTag(Tracing.Category, Tracing.Db);

			_syncHistoryTable.Value.ReplaceOne(item.Id, item, upsert: true);
		}
	}
}
