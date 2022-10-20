using Common.Dto.Garmin;
using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Service
{
	public class FileBasedSettingsService : ISettingsService
	{
		private static readonly ILogger _logger = LogContext.ForClass<FileBasedSettingsService>();
		private static readonly object _lock = new object();

		private const string GarminDeviceInfoKey = "GarminDeviceInfo";

		private readonly IConfiguration _configurationLoader;
		private readonly IMemoryCache _cache;
		private readonly IFileHandling _fileHandler;
		private readonly ISettingsService _next;

		public FileBasedSettingsService(IConfiguration configurationLoader, ISettingsService next, IMemoryCache cache, IFileHandling fileHandler)
		{
			_configurationLoader = configurationLoader;
			_next = next;
			_cache = cache;
			_fileHandler = fileHandler;
		}

		public void ClearGarminAuthentication(string garminEmail)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(ClearGarminAuthentication)}");

			_next.ClearGarminAuthentication(garminEmail);
		}

		public void ClearPelotonApiAuthentication(string pelotonEmail)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(ClearPelotonApiAuthentication)}");

			_next.ClearPelotonApiAuthentication(pelotonEmail);
		}

		public GarminApiAuthentication GetGarminAuthentication(string garminEmail)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(GetGarminAuthentication)}");

			return _next.GetGarminAuthentication(garminEmail);
		}

		public PelotonApiAuthentication GetPelotonApiAuthentication(string pelotonEmail)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(GetPelotonApiAuthentication)}");

			return _next.GetPelotonApiAuthentication(pelotonEmail);
		}

		public Task<Settings> GetSettingsAsync()
		{
			var settings = new Settings();
			ConfigurationSetup.LoadConfigValues(_configurationLoader, settings);

			return Task.FromResult(settings);
		}

		public void SetGarminAuthentication(GarminApiAuthentication authentication)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(SetGarminAuthentication)}");

			_next.SetGarminAuthentication(authentication);
		}

		public void SetPelotonApiAuthentication(PelotonApiAuthentication authentication)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(SetPelotonApiAuthentication)}");

			_next.SetPelotonApiAuthentication(authentication);
		}

		public Task UpdateSettingsAsync(Settings settings)
		{
			throw new NotImplementedException();
		}

		public Task<AppConfiguration> GetAppConfigurationAsync()
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(GetAppConfigurationAsync)}");

			return _next.GetAppConfigurationAsync();
		}

		public async Task<GarminDeviceInfo> GetCustomDeviceInfoAsync(string garminEmail)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(GetCustomDeviceInfoAsync)}");

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
	}
}
