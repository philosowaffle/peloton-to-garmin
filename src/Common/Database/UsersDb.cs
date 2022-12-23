using Common.Dto.P2G;
using Common.Observe;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Database;

public interface IUsersDb
{
	Task<ICollection<P2GUser>> GetUsersAsync();
	Task AddUserAsync(P2GUser user);
	Task RemoveUserAsync(P2GUser user);
}

public class UsersDb : DbBase<P2GUser>, IUsersDb
{
	private static readonly ILogger _logger = LogContext.ForClass<UsersDb>();
	private static readonly P2GUser _defaultUser = new P2GUser() { Id = 1, UserName = "Admin" };
	private static readonly string UsersCollections = "users";

	private readonly DataStore _db;

	public UsersDb(IFileHandling fileHandler) : base("Users", fileHandler)
	{
		_db = new DataStore(DbPath);
	}

	public Task AddUserAsync(P2GUser user)
	{
		throw new NotImplementedException();
	}

	public async Task<ICollection<P2GUser>> GetUsersAsync()
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("get", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(SettingsDb)}.{nameof(GetUsersAsync)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			var users = _db.GetCollection<P2GUser>(UsersCollections);

			if (users.Count <= 0)
			{
				var success = await users.InsertOneAsync(_defaultUser);
				if (!success)
				{
					_logger.Error("Failed to save default User to Db.");
					return new List<P2GUser>() { _defaultUser };
				}
			}

			return users.AsQueryable().ToList();
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to get users from db");
			throw;
		}
	}

	public Task RemoveUserAsync(P2GUser user)
	{
		throw new NotImplementedException();
	}
}
