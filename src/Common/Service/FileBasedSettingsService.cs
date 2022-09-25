using Common.Observe;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Common.Service
{
	public class FileBasedSettingsService : ISettingsService
	{
		private static readonly ILogger _logger = LogContext.ForClass<FileBasedSettingsService>();

		private readonly IConfiguration _configurationLoader;
		private readonly ISettingsService _next;

		public FileBasedSettingsService(IConfiguration configurationLoader, ISettingsService next)
		{
			_configurationLoader = configurationLoader;
			_next = next;
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
	}
}
