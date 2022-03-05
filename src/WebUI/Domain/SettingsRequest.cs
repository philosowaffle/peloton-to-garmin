
using Common;

public class SettingsGetResponse
{
	public SettingsGetResponse()
	{
		App = new AppConfiguration();
		Settings = new Settings();
	}

	public AppConfiguration App { get; set; }
	public Settings Settings { get; set; }
}