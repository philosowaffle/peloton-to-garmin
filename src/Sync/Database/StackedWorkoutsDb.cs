using Common;
using Common.Database;
using Common.Observe;
using Common.Dto.P2G;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sync.Database;

public interface IStackedWorkoutsDb
{
	Task<ICollection<WorkoutStackRecord>> GetStackedWorkoutsRecords(int userId);
	Task UpsertWorkoutStackRecord(int userId, WorkoutStackRecord record);
	Task UpsertWorkoutStacksRecord(int userId, ICollection<WorkoutStackRecord> records);
	Task DeleteWorkoutStackRecord(int userId, string id);
	Task ClearAllData(int userId);
}

public class StackedWorkoutsDb : DbBase<WorkoutStackRecord>, IStackedWorkoutsDb
{
	private static readonly ILogger _logger = LogContext.ForClass<StackedWorkoutsDb>();

	private readonly DataStore _db;

	public StackedWorkoutsDb(IFileHandling fileHandling) : base("SyncStatus", fileHandling)
	{
		_db = new DataStore(DbPath);
	}

	public async Task ClearAllData(int userId)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("delete", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(StackedWorkoutsDb)}.{nameof(ClearAllData)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			var records = _db.GetCollection<WorkoutStackRecord>(userId.ToString());
			await records.DeleteManyAsync(r => true);
		}
		catch (Exception e)
		{
			_logger.Error(e, $"Failed to delete Stacked Workouts for user: {userId}");
			return;
		}
	}

	public async Task DeleteWorkoutStackRecord(int userId, string id)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("delete", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(StackedWorkoutsDb)}.{nameof(DeleteWorkoutStackRecord)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			var records = _db.GetCollection<WorkoutStackRecord>(userId.ToString());
			await records.DeleteOneAsync(id);
		}
		catch (Exception e)
		{
			_logger.Error(e, $"Failed to delete Workout Stack record {id} for user: {userId}");
			return;
		}
	}

	public Task<ICollection<WorkoutStackRecord>> GetStackedWorkoutsRecords(int userId)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(StackedWorkoutsDb)}.{nameof(GetStackedWorkoutsRecords)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			var records = _db.GetCollection<WorkoutStackRecord>(userId.ToString());
			return Task.FromResult<ICollection<WorkoutStackRecord>>(records.AsQueryable().ToList());
		} 
		catch (Exception e)
		{
			_logger.Error(e, $"Failed to get Workout Stacks from db for user: {userId}");
			return Task.FromResult<ICollection<WorkoutStackRecord>>(new List<WorkoutStackRecord>());
		}
	}

	public async Task UpsertWorkoutStackRecord(int userId, WorkoutStackRecord record)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsert", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(StackedWorkoutsDb)}.{nameof(UpsertWorkoutStackRecord)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			var records = _db.GetCollection<WorkoutStackRecord>(userId.ToString());

			if (await records.UpdateOneAsync(record.Id, record))
				return;

			await records.InsertOneAsync(record);
		}
		catch (Exception e)
		{
			_logger.Error(e, $"Failed to upsert Workout Stack to db for user: {userId} and workoutId: {record.Id}");
			return;
		}
	}

	public async Task UpsertWorkoutStacksRecord(int userId, ICollection<WorkoutStackRecord> newRecords)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsert", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(StackedWorkoutsDb)}.{nameof(UpsertWorkoutStacksRecord)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			var records = _db.GetCollection<WorkoutStackRecord>(userId.ToString());

			foreach (var record in newRecords)
			{
				if (await records.UpdateOneAsync(record.Id, record))
					continue;

				await records.InsertOneAsync(record);
			}
		}
		catch (Exception e)
		{
			_logger.Error(e, $"Failed to upsert Workout Stacks to db for user: {userId}");
			return;
		}
	}
}
