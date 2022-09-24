namespace Common.Dto.Api;

public class SettingsGetResponse
{
	public SettingsGetResponse()
	{
		App = new App();
		Format = new Format();
		Peloton = new SettingsPelotonGetResponse();
		Garmin = new SettingsGarminGetResponse();
	}

	public SettingsGetResponse(Settings settings)
	{
		App = settings.App;
		Format = settings.Format;

		Peloton = new SettingsPelotonGetResponse()
		{
			Email = settings.Peloton.Email,
			Password = settings.Peloton.Password,
			ExcludeWorkoutTypes = settings.Peloton.ExcludeWorkoutTypes,
			NumWorkoutsToDownload = settings.Peloton.NumWorkoutsToDownload,
			IsPasswordSet = !string.IsNullOrEmpty(settings.Peloton.Password)
		};

		Garmin = new SettingsGarminGetResponse()
		{
			Email = settings.Garmin.Email,
			Password = settings.Garmin.Password,
			FormatToUpload = settings.Garmin.FormatToUpload,
			Upload = settings.Garmin.Upload,
			UploadStrategy = settings.Garmin.UploadStrategy,
			IsPasswordSet = !string.IsNullOrEmpty(settings.Garmin.Password)
		};
	}

	public App App { get; set; }
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
