using Common;

namespace WebApp.Models
{
	public class SettingsGetResponse
	{ 
		public AppConfiguration App { get; set; }
		public Settings Settings { get; set; }
	}
}
