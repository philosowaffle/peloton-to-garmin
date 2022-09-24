using Common.Database;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Service;

public interface ISettingsService
{
	Task<Settings> GetSettingsAsync();
	Task UpdateSettingsAsync(Settings settings);
	PelotonApiAuthentication GetPelotonApiAuthentication(string pelotonEmail);
	void SetPelotonApiAuthentication(PelotonApiAuthentication authentication);
	void ClearPelotonApiAuthentication(string pelotonEmail);
}

public class SettingsService : ISettingsService
{
	private static readonly ILogger _logger = LogContext.ForClass<SettingsService>();
	private static readonly object _lock = new object();
	private static readonly string PelotonApiAuthKey = "PelotonApiAuth";

	private readonly ISettingsDb _db;
	private readonly IMemoryCache _cache;

	public SettingsService(ISettingsDb db, IMemoryCache cache)
	{
		_db = db;
		_cache = cache;
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

	public async Task<Settings> GetSettingsAsync()
	{
		using var tracing = Tracing.Trace($"{nameof(SettingsService)}.{nameof(GetSettingsAsync)}");

		return (await _db.GetSettingsAsync()) ?? new Settings();
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
		await _db.UpsertSettingsAsync(updatedSettings);
	}
}
