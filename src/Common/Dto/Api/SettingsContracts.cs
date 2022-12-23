using System.Collections.Generic;

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
			Password = null,
			ExcludeWorkoutTypes = settings.Peloton.ExcludeWorkoutTypes,
			NumWorkoutsToDownload = settings.Peloton.NumWorkoutsToDownload,
			IsPasswordSet = !string.IsNullOrEmpty(settings.Peloton.Password)
		};

		Garmin = new SettingsGarminGetResponse()
		{
			Email = settings.Garmin.Email,
			Password = null,
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

public class SettingsGarminGetResponse
{
	public bool IsPasswordSet { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
	public UploadStrategy UploadStrategy { get; set; }
}

public class SettingsGarminPostRequest
{
	public string Email { get; set; }
	public string Password { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
	public UploadStrategy UploadStrategy { get; set; }
}

public class SettingsPelotonGetResponse
{
	public bool IsPasswordSet { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public ICollection<WorkoutType> ExcludeWorkoutTypes { get; set; }
	public int NumWorkoutsToDownload { get; set; }
}

public class SettingsPelotonPostRequest
{
	public string Email { get; set; }
	public string Password { get; set; }
	public ICollection<WorkoutType> ExcludeWorkoutTypes { get; set; }
	public int NumWorkoutsToDownload { get; set; }
}

public static class Mapping
{
	public static SettingsPelotonPostRequest Map(this SettingsPelotonGetResponse response)
	{
		return new SettingsPelotonPostRequest()
		{
			Email = response.Email,
			Password = response.Password,
			ExcludeWorkoutTypes = response.ExcludeWorkoutTypes,
			NumWorkoutsToDownload = response.NumWorkoutsToDownload
		};
	}

	public static Common.Peloton Map(this SettingsPelotonPostRequest request)
	{
		return new Common.Peloton()
		{
			Email = request.Email,
			Password = request.Password,
			ExcludeWorkoutTypes = request.ExcludeWorkoutTypes,
			NumWorkoutsToDownload = request.NumWorkoutsToDownload,
		};
	}

	public static SettingsGarminPostRequest Map(this SettingsGarminGetResponse response)
	{
		return new SettingsGarminPostRequest()
		{
			Email = response.Email,
			Password = response.Password,
			FormatToUpload = response.FormatToUpload,
			Upload = response.Upload,
			UploadStrategy = response.UploadStrategy,
		};
	}

	public static Common.Garmin Map(this SettingsGarminPostRequest request)
	{
		return new Common.Garmin()
		{
			Email = request.Email,
			Password = request.Password,
			FormatToUpload = request.FormatToUpload,
			Upload = request.Upload,
			UploadStrategy = request.UploadStrategy,
		};
	}
}