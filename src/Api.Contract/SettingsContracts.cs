using Common;
using Common.Dto;

namespace Api.Contract;

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
			TwoStepVerificationEnabled = settings.Garmin.TwoStepVerificationEnabled,
			FormatToUpload = settings.Garmin.FormatToUpload,
			Upload = settings.Garmin.Upload,
			IsPasswordSet = !string.IsNullOrEmpty(settings.Garmin.Password),
			Api = settings.Garmin.Api ?? new GarminApiSettings()
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
	public string? Email { get; set; }
	public string? Password { get; set; }
	public bool TwoStepVerificationEnabled { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
	public GarminApiSettings Api { get; set; } = new GarminApiSettings();
}

public class SettingsGarminPostRequest
{
	public string? Email { get; set; }
	public string? Password { get; set; }
	public bool TwoStepVerificationEnabled { get; set; }
	public bool Upload { get; set; }
	public FileFormat FormatToUpload { get; set; }
	public GarminApiSettings Api { get; set; } = new GarminApiSettings();
}

public class SettingsPelotonGetResponse
{
	public SettingsPelotonGetResponse() 
	{
		ExcludeWorkoutTypes = new List<WorkoutType>();
	}

	public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && IsPasswordSet;
	public bool IsPasswordSet { get; set; }
	public string? Email { get; set; }
	public string? Password { get; set; }
	public ICollection<WorkoutType> ExcludeWorkoutTypes { get; set; }
	public int NumWorkoutsToDownload { get; set; }
}

public class SettingsPelotonPostRequest
{
	public string? Email { get; set; }
	public string? Password { get; set; }
	public ICollection<WorkoutType>? ExcludeWorkoutTypes { get; set; }
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

	public static PelotonSettings Map(this SettingsPelotonPostRequest request)
	{
		return new ()
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
			TwoStepVerificationEnabled = response.TwoStepVerificationEnabled,
			FormatToUpload = response.FormatToUpload,
			Upload = response.Upload,
			Api = response.Api,
		};
	}

	public static GarminSettings Map(this SettingsGarminPostRequest request)
	{
		return new ()
		{
			Email = request.Email,
			Password = request.Password,
			TwoStepVerificationEnabled = request.TwoStepVerificationEnabled,
			FormatToUpload = request.FormatToUpload,
			Upload = request.Upload,
			Api = request.Api,
		};
	}
}