using Common.Database;
using Common.Dto.Garmin;
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
	private static readonly string GarminApiAuthKey = "GarminApiAuth";
	private static readonly string GarminDeviceInfoKey = "GarminDeviceInfo";

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

		return (await _db.GetSettingsAsync(1)) ?? new Settings(); // hardcode to admin user for now
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

		ClearGarminAuthentication(originalSettings.Garmin.Email);
		ClearGarminAuthentication(originalSettings.Garmin.Password);

		ClearCustomDeviceInfoAsync(originalSettings.Garmin.Email);
		ClearCustomDeviceInfoAsync(updatedSettings.Garmin.Email);

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

	public GarminApiAuthentication GetGarminAuthentication(string garminEmail)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetGarminAuthentication)}");

		lock (_lock)
		{
			var key = $"{GarminApiAuthKey}:{garminEmail}";
			return _cache.Get<GarminApiAuthentication>(key);
		}
	}

	public void SetGarminAuthentication(GarminApiAuthentication authentication)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(SetGarminAuthentication)}");

		lock (_lock)
		{
			var key = $"{GarminApiAuthKey}:{authentication.Email}";
			_cache.Set(key, authentication, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
		}
	}

	public void ClearGarminAuthentication(string garminEmail)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(ClearGarminAuthentication)}");

		lock (_lock)
		{
			var key = $"{GarminApiAuthKey}:{garminEmail}";
			_cache.Remove(key);
		}
	}

	public Task<AppConfiguration> GetAppConfigurationAsync()
	{
		var appConfiguration = new AppConfiguration();
		ConfigurationSetup.LoadConfigValues(_configurationLoader, appConfiguration);

		return Task.FromResult(appConfiguration);
	}

	public async Task<GarminDeviceInfo> GetCustomDeviceInfoAsync(string garminEmail)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetCustomDeviceInfoAsync)}");

		GarminDeviceInfo userProvidedDeviceInfo = null;

		var settings = await GetSettingsAsync();
		var userDevicePath = settings.Format.DeviceInfoPath;

		if (string.IsNullOrEmpty(userDevicePath))
			return null;

		lock (_lock)
		{
			var key = $"{GarminDeviceInfoKey}:{garminEmail}";
			return _cache.GetOrCreate(key, (cacheEntry) => 
			{
				cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
				if (_fileHandler.TryDeserializeXml(userDevicePath, out userProvidedDeviceInfo))
					return userProvidedDeviceInfo;

				return null;
			});
		}
	}

	private void ClearCustomDeviceInfoAsync(string garminEmail)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(ClearCustomDeviceInfoAsync)}");

		lock (_lock)
		{
			var key = $"{GarminDeviceInfoKey}:{garminEmail}";
			_cache.Remove(key);
		}
	}
}
