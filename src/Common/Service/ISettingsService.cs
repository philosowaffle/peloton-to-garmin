using Common.Dto.Garmin;
using Common.Stateful;
using System.Threading.Tasks;

namespace Common.Service
{
	public interface ISettingsService
	{
		Task<Settings> GetSettingsAsync();
		Task UpdateSettingsAsync(Settings settings);

		Task<AppConfiguration> GetAppConfigurationAsync();

		Task<GarminDeviceInfo> GetCustomDeviceInfoAsync(string garminEmail);

		PelotonApiAuthentication GetPelotonApiAuthentication(string pelotonEmail);
		void SetPelotonApiAuthentication(PelotonApiAuthentication authentication);
		void ClearPelotonApiAuthentication(string pelotonEmail);

		GarminApiAuthentication GetGarminAuthentication(string garminEmail);
		void SetGarminAuthentication(GarminApiAuthentication authentication);
		void ClearGarminAuthentication(string garminEmail);
	}
}
