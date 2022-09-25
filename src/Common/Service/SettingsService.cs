using Common.Database;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Service;

public interface ISettingsService
{
	Task<Settings> GetSettingsAsync();
	Task UpdateSettingsAsync(Settings settings);

	Task<AppConfiguration> GetAppConfigurationAsync();

	PelotonApiAuthentication GetPelotonApiAuthentication(string pelotonEmail);
	void SetPelotonApiAuthentication(PelotonApiAuthentication authentication);
	void ClearPelotonApiAuthentication(string pelotonEmail);

	GarminApiAuthentication GetGarminAuthentication(string garminEmail);
	void SetGarminAuthentication(GarminApiAuthentication authentication);
	void ClearGarminAuthentication(string garminEmail);
}

public class SettingsService : ISettingsService
{
	private static readonly ILogger _logger = LogContext.ForClass<SettingsService>();
	private static readonly object _lock = new object();
	private static readonly string PelotonApiAuthKey = "PelotonApiAuth";
	private static readonly string GarminApiAuthKey = "GarminApiAuth";

	private readonly ISettingsDb _db;
	private readonly IMemoryCache _cache;
	private readonly IConfiguration _configurationLoader;

	public SettingsService(ISettingsDb db, IMemoryCache cache, IConfiguration configurationLoader)
	{
		_db = db;
		_cache = cache;
		_configurationLoader = configurationLoader;
	}

	public async Task<Settings> GetSettingsAsync()
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetSettingsAsync)}");

		return (await _db.GetSettingsAsync()) ?? new Settings();
	}

	public async Task UpdateSettingsAsync(Settings updatedSettings)
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(UpdateSettingsAsync)}");

		var originalSettings = await _db.GetSettingsAsync();

		if (updatedSettings.Garmin.Password is null)
			updatedSettings.Garmin.Password = originalSettings.Garmin.Password;

		if (updatedSettings.Peloton.Password is null)
			updatedSettings.Peloton.Password = originalSettings.Peloton.Password;

		ClearPelotonApiAuthentication(originalSettings.Peloton.Email);
		ClearPelotonApiAuthentication(updatedSettings.Peloton.Email);

		ClearGarminAuthentication(originalSettings.Garmin.Email);
		ClearGarminAuthentication(originalSettings.Garmin.Password);

		await _db.UpsertSettingsAsync(updatedSettings);
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
}
