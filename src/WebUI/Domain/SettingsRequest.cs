
using Common;

namespace WebUI.Domain;

public class SettingsGetResponse
{
	public SettingsGetResponse()
	{
		App = new Common.App();
		Format = new Format();
		Peloton = new SettingsPelotonGetResponse();
		Garmin = new SettingsGarminGetResponse();
	}

	public Common.App App { get; set; }
	public Format Format { get; set; }
	public SettingsPelotonGetResponse Peloton { get; set; }
	public SettingsGarminGetResponse Garmin { get; set; }
}

public class SettingsGarminGetResponse : Common.Garmin
{
	public bool IsPasswordSet { get; set; }
}

public class SettingsPelotonGetResponse : Common.Peloton
{
	public bool IsPasswordSet { get; set; }
}