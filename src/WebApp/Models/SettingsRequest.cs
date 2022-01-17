using Common;
using Common.Helpers;

namespace WebApp.Models
{
	public class SettingsGetResponse
	{ 
		public AppConfiguration App { get; set; }
		public Settings Settings { get; set; }
	}

	public class SettingsViewModel
    {
		public SettingsViewModel()
        {
			GetResponse = new SettingsGetResponse();
			Copy = new Copy();
        }

		public SettingsGetResponse GetResponse { get; set; }

		public Copy Copy { get; set; }
    }

	public class Copy
    {
		public Copy()
        {
			Settings = new SettingsCopy();
        }

		public SettingsCopy Settings { get; set; }
    }

	public class SettingsCopy
    {
		public SettingsCopy()
        {
			AppOutputDirectory = typeof(App).GetFieldDescription("OutputDirectory");
			AppWorkingDirectory = typeof(App).GetFieldDescription("WorkingDirectory");
			AppEnablePolling = typeof(App).GetFieldDescription("EnablePolling");
			AppPollingIntervalSeconds = typeof(App).GetFieldDescription("PollingIntervalSeconds");

			FormatFit = typeof(Format).GetFieldDescription("Fit");
			FormatJson = typeof(Format).GetFieldDescription("Json");
			FormatTcx = typeof(Format).GetFieldDescription("Tcx");
			FormatSaveLocalCopy = typeof(Format).GetFieldDescription("SaveLocalCopy");
			FormatIncludeTimeInHRZones = typeof(Format).GetFieldDescription("IncludeTimeInHRZones");
			FormatIncludeTimeInPowerZones = typeof(Format).GetFieldDescription("IncludeTimeInPowerZones");
			FormatDeviceInfoPath = typeof(Format).GetFieldDescription("DeviceInfoPath");

			FormatRunningPreferredLapType = typeof(Running).GetFieldDescription("PreferredLapType");
			FormatCyclingPreferredLapType = typeof(Cycling).GetFieldDescription("PreferredLapType");

			PelotonExcludeWorkoutTypes = typeof(Common.Peloton).GetFieldDescription("ExcludeWorkoutTypes");
		}

		public string AppOutputDirectory { get; set; }
		public string AppWorkingDirectory { get; set; }
		public string AppEnablePolling { get; set; }
		public string AppPollingIntervalSeconds { get; set; }

		public string FormatFit { get; set; }
		public string FormatJson { get; set; }
		public string FormatTcx { get; set; }
		public string FormatSaveLocalCopy { get; set; }
		public string FormatIncludeTimeInHRZones { get; set; }
		public string FormatIncludeTimeInPowerZones { get; set; }
		public string FormatDeviceInfoPath { get; set; }

		public string FormatRunningPreferredLapType { get; set; }
		public string FormatCyclingPreferredLapType { get; set; }

		public string PelotonExcludeWorkoutTypes { get; set; }

	}
}
