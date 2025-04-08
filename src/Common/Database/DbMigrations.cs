using Common.Dto;
using Common.Dto.Garmin;
using Common.Observe;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Database;

#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete

public interface IDbMigrations
{
	Task PreformMigrations();
	Task MigrateDeviceInfoFileToListAsync();
}

public class DbMigrations : IDbMigrations
{
	private static readonly ILogger _logger = LogContext.ForClass<DbMigrations>();

	private readonly ISettingsDb _settingsDb;
	private readonly IUsersDb _usersDb;
	private readonly IFileHandling _fileHandler;

	public DbMigrations(ISettingsDb settingsDb, IUsersDb usersDb, IFileHandling fileHandler)
	{
		_settingsDb = settingsDb;
		_usersDb = usersDb;
		_fileHandler = fileHandler;
	}

	public async Task PreformMigrations()
	{
		await MigrateToAdminUserAsync();
		await MigrateToEncryptedCredentialsAsync();
		await MigrateDeviceInfoFileToListAsync();
	}

	/// <summary>
	/// P2G 3.3.0
	/// </summary>
	public async Task MigrateToAdminUserAsync()
	{
		var legacySettings = _settingsDb.DbMigrations_TryGetLegacy_Settings();

		if (legacySettings is null) return;

		_logger.Information("[MIGRATION] Migrating settings to Admin user...");

		var users = await _usersDb.GetUsersAsync();
		var admin = users.First();

		try
		{
			var success = await _settingsDb.UpsertSettingsAsync(admin.Id, legacySettings);
			if (success)
			{
				await _settingsDb.DbMigrations_TryRemoveLegacySettingsAsync();
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
	}

	/// <summary>
	/// P2G 3.3.0
	/// </summary>
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

	/// <summary>
	/// P2G 4.2.0
	/// </summary>
	public async Task MigrateDeviceInfoFileToListAsync()
	{

		var admin = (await _usersDb.GetUsersAsync()).First();
		var settings = await _settingsDb!.GetSettingsAsync(admin.Id);
		var legacyDeviceInfoSettings = await _settingsDb!.DbMigrations_TryGetLegacy_DeviceInfoSettings(admin.Id);

		if (string.IsNullOrWhiteSpace(legacyDeviceInfoSettings.DeviceInfoPath))
			return;

		_logger.Information($"[MIGRATION] Migrating {legacyDeviceInfoSettings.DeviceInfoPath} to new settings format.");

		try
		{
			GarminDeviceInfo deviceInfo = null;
			_fileHandler.TryDeserializeXml(legacyDeviceInfoSettings.DeviceInfoPath, out deviceInfo);

			if (deviceInfo != null)
			{
				settings.Format.DeviceInfoSettings.Clear();
				settings.Format.DeviceInfoSettings.Add(WorkoutType.None, deviceInfo);
				
				legacyDeviceInfoSettings.DeviceInfoPath = null;

				await _settingsDb.UpsertSettingsAsync(admin.Id, settings);
			} 
			else
			{
				_logger.Warning($"[MIGRATION] Failed to parse {legacyDeviceInfoSettings.DeviceInfoPath}, migrating to P2G default device settings instead.");
				settings.Format.DeviceInfoSettings = new Dictionary<WorkoutType, GarminDeviceInfo>()
				{
					{ WorkoutType.None, GarminDevices.Forerunner945 },
					{ WorkoutType.Cycling, GarminDevices.TACXDevice },
					{ WorkoutType.Rowing, GarminDevices.EpixDevice },
				};
				
				legacyDeviceInfoSettings.DeviceInfoPath = null;

				await _settingsDb.UpsertSettingsAsync(admin.Id, settings);
			}

			_logger.Information($"[MIGRATION] Successfully migrated {legacyDeviceInfoSettings.DeviceInfoPath} to new settings format.");

		} catch (Exception e)
		{
			_logger.Error(e, $"[MIGRATION] Failed to migrated {legacyDeviceInfoSettings.DeviceInfoPath} to new settings format.");
		}
	}

}

/// <summary>
/// P2G 5.0.0
/// </summary>
public class DbMigrations_LegacyDeviceInfo
{
	public string DeviceInfoPath { get; set; }
}

#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete