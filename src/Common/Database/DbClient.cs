using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PromMetrics = Prometheus.Metrics;

namespace Common.Database
{
	public interface IDbClient
	{
		SyncHistoryItem Get(string id);
		ICollection<SyncHistoryItem> GetRecentlySyncedItems(int limit);
		void Upsert(SyncHistoryItem item);
	}

	public class DbClient : IDbClient
	{
		private static readonly Histogram DbActionDuration = PromMetrics.CreateHistogram("p2g_db_duration_seconds", "Counter of db actions.", new HistogramConfiguration()
		{
			LabelNames = new[] { Metrics.Label.DbMethod, Metrics.Label.DbQuery }
		});
		private static readonly ILogger _logger = LogContext.ForClass<DbClient>();

		private Lazy<IDocumentCollection<SyncHistoryItem>> _syncHistoryTable;
		private IFileHandling _fileHandler;

		public DbClient(IAppConfiguration configuration, IFileHandling fileHandler)
		{
			_fileHandler = fileHandler;

			MakeDbIfNotExist(configuration.App.SyncHistoryDbPath);

			var syncHistoryDb = new DataStore(configuration.App.SyncHistoryDbPath);
			_syncHistoryTable = new Lazy<IDocumentCollection<SyncHistoryItem>>(() => syncHistoryDb.GetCollection<SyncHistoryItem>());		
		}

		private void MakeDbIfNotExist(string dbPath)
		{
			if (!File.Exists(dbPath))
			{
				_logger.Debug("Creating db: {@Path}", dbPath);
				try
				{
					var dir = Path.GetDirectoryName(dbPath);
					_fileHandler.MkDirIfNotExists(dir);
					File.WriteAllText(dbPath, "{}");

				}
				catch (Exception e)
				{
					_logger.Fatal(e, "Failed to create db file: {@Path}", dbPath);
					throw;
				}
			}
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

		public ICollection<SyncHistoryItem> GetRecentlySyncedItems(int limit)
		{
			using var metrics = DbActionDuration
									.WithLabels("select", "workoutIds")
									.NewTimer();
			using var tracing = Tracing.Trace("select", TagValue.Db)
										.WithTable("SyncHistoryItem");

			try
			{
				return _syncHistoryTable.Value
					.AsQueryable()
					.OrderByDescending(i => i.WorkoutDate)
					.ThenByDescending(i => i.UploadDate)
					.Take(limit)
					.ToList();
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to get recently synced items.");
				return null;
			}
		}
	}
}
