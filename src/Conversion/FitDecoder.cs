using Common;
using Common.Observe;
using Dynastream.Fit;
using Serilog;
using System.IO;

namespace Conversion
{
	public static class FitDecoder
	{
		private static readonly ILogger _logger = LogContext.ForStatic("FitDecoder");

		public static void Decode(string filePath)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Decode)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			Decode decoder = new Decode();
			MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();

			decoder.MesgEvent += mesgBroadcaster.OnMesg;
			decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;
			decoder.DeveloperFieldDescriptionEvent += Write;

			mesgBroadcaster.AccelerometerDataMesgEvent += Write;
			mesgBroadcaster.ActivityMesgEvent += Write;
			mesgBroadcaster.AntChannelIdMesgEvent += Write;
			mesgBroadcaster.AntRxMesgEvent += Write;
			mesgBroadcaster.AntTxMesgEvent += Write;
			mesgBroadcaster.AviationAttitudeMesgEvent += Write;
			mesgBroadcaster.BarometerDataMesgEvent += Write;
			mesgBroadcaster.BikeProfileMesgEvent += Write;
			mesgBroadcaster.BloodPressureMesgEvent += Write;
			mesgBroadcaster.CadenceZoneMesgEvent += Write;
			mesgBroadcaster.CameraEventMesgEvent += Write;
			mesgBroadcaster.CapabilitiesMesgEvent += Write;
			mesgBroadcaster.ClimbProMesgEvent += Write;
			mesgBroadcaster.ConnectivityMesgEvent += Write;
			mesgBroadcaster.CourseMesgEvent += Write;
			mesgBroadcaster.CoursePointMesgEvent += Write;
			mesgBroadcaster.DeveloperDataIdMesgEvent += Write;
			mesgBroadcaster.DeviceInfoMesgEvent += Write;
			mesgBroadcaster.DeviceSettingsMesgEvent += Write;
			mesgBroadcaster.DiveAlarmMesgEvent += Write;
			mesgBroadcaster.DiveGasMesgEvent += Write;
			mesgBroadcaster.DiveSettingsMesgEvent += Write;
			mesgBroadcaster.DiveSummaryMesgEvent += Write;
			mesgBroadcaster.EventMesgEvent += Write;
			mesgBroadcaster.ExdDataConceptConfigurationMesgEvent += Write;
			mesgBroadcaster.ExdDataFieldConfigurationMesgEvent += Write;
			mesgBroadcaster.ExdScreenConfigurationMesgEvent += Write;
			mesgBroadcaster.ExerciseTitleMesgEvent += Write;
			mesgBroadcaster.FieldCapabilitiesMesgEvent += Write;
			mesgBroadcaster.FieldDescriptionMesgEvent += Write;
			mesgBroadcaster.FileCapabilitiesMesgEvent += Write;
			mesgBroadcaster.FileCreatorMesgEvent += Write;
			mesgBroadcaster.FileIdMesgEvent += Write;
			mesgBroadcaster.GoalMesgEvent += Write;
			mesgBroadcaster.GpsMetadataMesgEvent += Write;
			mesgBroadcaster.GyroscopeDataMesgEvent += Write;
			mesgBroadcaster.HrMesgEvent += Write;
			mesgBroadcaster.HrmProfileMesgEvent += Write;
			mesgBroadcaster.HrvMesgEvent += Write;
			mesgBroadcaster.HrZoneMesgEvent += Write;
			mesgBroadcaster.JumpMesgEvent += Write;
			mesgBroadcaster.LapMesgEvent += Write;
			mesgBroadcaster.LengthMesgEvent += Write;
			mesgBroadcaster.MagnetometerDataMesgEvent += Write;
			mesgBroadcaster.MemoGlobMesgEvent += Write;
			mesgBroadcaster.MesgCapabilitiesMesgEvent += Write;
			mesgBroadcaster.MesgEvent += Write;
			mesgBroadcaster.MetZoneMesgEvent += Write;
			mesgBroadcaster.MonitoringInfoMesgEvent += Write;
			mesgBroadcaster.MonitoringMesgEvent += Write;
			mesgBroadcaster.NmeaSentenceMesgEvent += Write;
			mesgBroadcaster.ObdiiDataMesgEvent += Write;
			mesgBroadcaster.OhrSettingsMesgEvent += Write;
			mesgBroadcaster.OneDSensorCalibrationMesgEvent += Write;
			mesgBroadcaster.PadMesgEvent += Write;
			mesgBroadcaster.PowerZoneMesgEvent += Write;
			mesgBroadcaster.RecordMesgEvent += Write;
			mesgBroadcaster.ScheduleMesgEvent += Write;
			mesgBroadcaster.SdmProfileMesgEvent += Write;
			mesgBroadcaster.SegmentFileMesgEvent += Write;
			mesgBroadcaster.SegmentIdMesgEvent += Write;
			mesgBroadcaster.SegmentLapMesgEvent += Write;
			mesgBroadcaster.SegmentLeaderboardEntryMesgEvent += Write;
			mesgBroadcaster.SegmentPointMesgEvent += Write;
			mesgBroadcaster.SessionMesgEvent += Write;
			mesgBroadcaster.SlaveDeviceMesgEvent += Write;
			mesgBroadcaster.SoftwareMesgEvent += Write;
			mesgBroadcaster.SpeedZoneMesgEvent += Write;
			mesgBroadcaster.SportMesgEvent += Write;
			mesgBroadcaster.StressLevelMesgEvent += Write;
			mesgBroadcaster.ThreeDSensorCalibrationMesgEvent += Write;
			mesgBroadcaster.TimestampCorrelationMesgEvent += Write;
			mesgBroadcaster.TotalsMesgEvent += Write;
			mesgBroadcaster.TrainingFileMesgEvent += Write;
			mesgBroadcaster.UserProfileMesgEvent += Write;
			mesgBroadcaster.VideoClipMesgEvent += Write;
			mesgBroadcaster.VideoDescriptionMesgEvent += Write;
			mesgBroadcaster.VideoFrameMesgEvent += Write;
			mesgBroadcaster.VideoMesgEvent += Write;
			mesgBroadcaster.VideoTitleMesgEvent += Write;
			mesgBroadcaster.WatchfaceSettingsMesgEvent += Write;
			mesgBroadcaster.WeatherAlertMesgEvent += Write;
			mesgBroadcaster.WeatherConditionsMesgEvent += Write;
			mesgBroadcaster.WeightScaleMesgEvent += Write;
			mesgBroadcaster.WorkoutMesgEvent += Write;
			mesgBroadcaster.WorkoutSessionMesgEvent += Write;
			mesgBroadcaster.WorkoutStepMesgEvent += Write;
			mesgBroadcaster.ZonesTargetMesgEvent += Write;

			FileStream fitDest = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			decoder.Read(fitDest);
		}

		private static void Write(object sender, MesgEventArgs e)
		{
			_logger.Verbose($"{e.mesg.Name}::");
			foreach(var f in e.mesg.Fields)
			{
				_logger.Verbose($"{f.GetName()}::{f.GetValue()}");
			}

			try
			{
				var dev = (DeveloperDataIdMesg)e.mesg;
				_logger.Verbose($"FOUND DEV FIELD - {dev.Name} {dev.Num}");
				foreach(DeveloperField devField in dev.DeveloperFields)
				{
					var name = devField.Name;
					var value = devField.GetValue();
					var units = devField.GetUnits();
					var isResistance = devField.NativeOverride == RecordMesg.FieldDefNum.Resistance;
					_logger.Verbose($"DevFields: {name} {value} {units} isResistance:{isResistance}");
				}

				foreach(Field devField in dev.Fields)
				{
					var name = devField.Name;
					var value = devField.GetValue();
					var units = devField.GetUnits();
					_logger.Verbose($"Fields: {name} {value} {units}");
				}
			}
			catch { }
		}

		private static void Write(object sender, DeveloperFieldDescriptionEventArgs e)
		{
			_logger.Verbose("DeveloperDescription");
			_logger.Verbose($"ApplicationId::{e.Description.ApplicationId}");
			_logger.Verbose($"ApplicationVersion::{e.Description.ApplicationVersion}");
			_logger.Verbose($"FieldDefinitionNumber::{e.Description.FieldDefinitionNumber}");
		}
	}
}
