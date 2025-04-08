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

		public void ClearPelotonApiAuthentication(string pelotonEmail)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(ClearPelotonApiAuthentication)}");

			_next.ClearPelotonApiAuthentication(pelotonEmail);
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

			if (settings.Format is null)
				settings.Format = new Settings().Format;

			if (settings.Format.DeviceInfoSettings is null)
				settings.Format.DeviceInfoSettings = Format.DefaultDeviceInfoSettings;

			if (!settings.Format.DeviceInfoSettings.TryGetValue(WorkoutType.None, out var _))
				settings.Format.DeviceInfoSettings.Add(WorkoutType.None, Format.DefaultDeviceInfoSettings[WorkoutType.None]);

			return Task.FromResult(settings);
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

		public async Task<GarminDeviceInfo> GetCustomDeviceInfoAsync(Workout workout)
		{
			using var tracing = Tracing.Trace($"{nameof(FileBasedSettingsService)}.{nameof(GetCustomDeviceInfoAsync)}");

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
}
