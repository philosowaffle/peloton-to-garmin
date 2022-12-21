using Common.Observe;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Database;

public interface IDbMigrations
{
	public Task PreformMigrations();
}

public class DbMigrations : IDbMigrations
{
	private static readonly ILogger _logger = LogContext.ForClass<DbMigrations>();

	private readonly ISettingsDb _settingsDb;
	private readonly IUsersDb _usersDb;
	private readonly ISyncStatusDb _syncStatusDb;

	public DbMigrations(ISettingsDb settingsDb, IUsersDb usersDb, ISyncStatusDb syncStatusDb)
	{
		_settingsDb = settingsDb;
		_usersDb = usersDb;
		_syncStatusDb = syncStatusDb;
	}

	public async Task PreformMigrations()
	{
		await MigrateToAdminUserAsync();
		await MigrateToEncryptedCredentialsAsync();
	}

	public async Task MigrateToAdminUserAsync()
	{
		#pragma warning disable CS0612 // Type or member is obsolete
		var legacySettings = _settingsDb.GetLegacySettings();

		if (legacySettings is null) return;

		_logger.Information("[MIGRATION] Migrating settings to Admin user...");

		var users = await _usersDb.GetUsersAsync();
		var admin = users.First();

		try
		{
			var success = await _settingsDb.UpsertSettingsAsync(admin.Id, legacySettings);
			if (success)
			{
				await _settingsDb.RemoveLegacySettingsAsync();
				_logger.Information("[MIGRATION] Successfully migrated existing data to new Admin user.");
			}
			else
			{
				_logger.Error("[MIGRATION] Failed to migrate existing settings to Admin user.");
			}
		}
		catch (Exception e)
		{
			_logger.Error(e, "[MIGRATION] Failed to migrate existing data to Admin user.");
		}

		try
		{
			await _syncStatusDb!.DeleteLegacySyncStatusAsync();
		}
		catch (Exception e)
		{
			_logger.Warning(e, "[MIGRATION Failed to delete LegacySyncStatus.");
		}
		#pragma warning restore CS0612 // Type or member is obsolete
	}

	public async Task MigrateToEncryptedCredentialsAsync()
	{
		var admin = (await _usersDb.GetUsersAsync()).First();
		var settings = await _settingsDb!.GetSettingsAsync(admin.Id);

		if (settings.Peloton.EncryptionVersion == EncryptionVersion.V1
			&& settings.Garmin.EncryptionVersion == EncryptionVersion.V1)
			return;

		_logger.Information("[MIGRATION] Encrypting Peloton and Garmin credentials...");

		try
		{
			await _settingsDb.UpsertSettingsAsync(admin.Id, settings);
			_logger.Information("[MIGRATION] Successfully encrypted Peloton and Garmin credentials.");
		}
		catch (Exception e)
		{
			_logger.Error(e, "[MIGRATION] Failed to encrypt Peloton and Garmin credentials.");
		}

	}
}