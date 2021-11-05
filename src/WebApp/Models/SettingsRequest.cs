using Common;

namespace WebApp.Models
{
	public class SettingsGetResponse : Configuration
	{
		public SettingsGetResponse(IAppConfiguration configuration)
		{
			App = configuration.App;
			Developer = configuration.Developer;
			Format = configuration.Format;
			Garmin = configuration.Garmin;
			Observability = configuration.Observability;
			Peloton = configuration.Peloton;
		}
	}
}
