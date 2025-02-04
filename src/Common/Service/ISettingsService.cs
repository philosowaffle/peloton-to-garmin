using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Stateful;
using System.Threading.Tasks;

namespace Common.Service
{
	public interface ISettingsService
	{
		Task<Settings> GetSettingsAsync();
		Task UpdateSettingsAsync(Settings settings);

		Task<AppConfiguration> GetAppConfigurationAsync();

		Task<GarminDeviceInfo> GetCustomDeviceInfoAsync(Workout workout);

		PelotonApiAuthentication GetPelotonApiAuthentication(string pelotonEmail);
		void SetPelotonApiAuthentication(PelotonApiAuthentication authentication);
		void ClearPelotonApiAuthentication(string pelotonEmail);
	}
}
