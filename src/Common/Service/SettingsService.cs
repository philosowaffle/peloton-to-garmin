using Common.Database;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Service;

public class SettingsService : ISettingsService
{
	private static readonly ILogger _logger = LogContext.ForClass<SettingsService>();
	private static readonly object _lock = new object();
	private static readonly string PelotonApiAuthKey = "PelotonApiAuth";

	private readonly ISettingsDb _db;
	private readonly IMemoryCache _cache;
	private readonly IConfiguration _configurationLoader;
	private readonly IFileHandling _fileHandler;

	public SettingsService(ISettingsDb db, IMemoryCache cache, IConfiguration configurationLoader, IFileHandling fileHandler)
	{
		_db = db;
		_cache = cache;
		_configurationLoader = configurationLoader;
		_fileHandler = fileHandler;
	}

	public async Task<Settings> GetSettingsAsync()
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetSettingsAsync)}");

		var settings = (await _db.GetSettingsAsync(1)) ?? new Settings(); // hardcode to admin user for now

		if (settings.Format is null)
			settings.Format = new Settings().Format;

		if (settings.Format.DeviceInfoSettings is null)
			settings.Format.DeviceInfoSettings = Format.DefaultDeviceInfoSettings;

		if (!settings.Format.DeviceInfoSettings.TryGetValue(WorkoutType.None, out var _))
			settings.Format.DeviceInfoSettings.Add(WorkoutType.None, Format.DefaultDeviceInfoSettings[WorkoutType.None]);

		return settings;
	}

	public async Task UpdateSettingsAsync(Settings updatedSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(UpdateSettingsAsync)}");

		var originalSettings = await _db.GetSettingsAsync(1); // hardcode to admin user for now

		if (updatedSettings.Garmin.Password is null)
			updatedSettings.Garmin.Password = originalSettings.Garmin.Password;

		if (updatedSettings.Peloton.Password is null)
			updatedSettings.Peloton.Password = originalSettings.Peloton.Password;

		ClearPelotonApiAuthentication(originalSettings.Peloton.Email);
		ClearPelotonApiAuthentication(updatedSettings.Peloton.Email);

		await _db.UpsertSettingsAsync(1, updatedSettings); // hardcode to admin user for now
	}

	public PelotonApiAuthentication GetPelotonApiAuthentication(string pelotonEmail)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetPelotonApiAuthentication)}");

		lock (_lock)
		{
			var key = $"{PelotonApiAuthKey}:{pelotonEmail}";
			return _cache.Get<PelotonApiAuthentication>(key);
		}
	}

	public void SetPelotonApiAuthentication(PelotonApiAuthentication authentication)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(SetPelotonApiAuthentication)}");

		lock (_lock)
		{
			var key = $"{PelotonApiAuthKey}:{authentication.Email}";
			_cache.Set(key, authentication, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
		}
	}

	public void ClearPelotonApiAuthentication(string pelotonEmail)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(ClearPelotonApiAuthentication)}");

		lock (_lock)
		{
			var key = $"{PelotonApiAuthKey}:{pelotonEmail}";
			_cache.Remove(key);
		}
	}

	public Task<AppConfiguration> GetAppConfigurationAsync()
	{
		var appConfiguration = new AppConfiguration();
		ConfigurationSetup.LoadConfigValues(_configurationLoader, appConfiguration);

		return Task.FromResult(appConfiguration);
	}

	public async Task<GarminDeviceInfo> GetCustomDeviceInfoAsync(Workout workout)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetCustomDeviceInfoAsync)}");

		var workoutType = WorkoutType.None;
		if (workout is object)
			workoutType = workout.GetWorkoutType();

		GarminDeviceInfo userProvidedDeviceInfo = null;

		var settings = await GetSettingsAsync();

		if (settings?.Format?.DeviceInfoSettings is object)
		{
			settings.Format.DeviceInfoSettings.TryGetValue(workoutType, out userProvidedDeviceInfo);

			if (userProvidedDeviceInfo is null)
				settings.Format.DeviceInfoSettings.TryGetValue(WorkoutType.None, out userProvidedDeviceInfo);
		}

		if (userProvidedDeviceInfo is null)
		{
			Format.DefaultDeviceInfoSettings.TryGetValue(workoutType, out userProvidedDeviceInfo);

			if (userProvidedDeviceInfo is null)
				Format.DefaultDeviceInfoSettings.TryGetValue(WorkoutType.None, out userProvidedDeviceInfo);
		}

		return userProvidedDeviceInfo;
	}
}
