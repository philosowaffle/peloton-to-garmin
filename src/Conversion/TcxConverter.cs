using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Serilog;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Conversion
{
	public class TcxConverter : Converter<XElement>
	{
		private static readonly ILogger _logger = LogContext.ForClass<TcxConverter>();

		public TcxConverter(Settings settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

		public override ConvertStatus Convert(P2GWorkout workout)
		{
			if (!_config.Format.Tcx) return new ConvertStatus() { Success = true, ErrorMessage = "Tcx format disabled in config." };

			return base.ConvertForFormat(FileFormat.Fit, workout);
		}

		protected override void Save(XElement data, string path)
		{
			using var tracing = Tracing.Trace($"{nameof(TcxConverter)}.{nameof(Save)}")
								.WithTag(TagKey.Format, FileFormat.Tcx.ToString());

			data.Save(path);
		}

		protected override void SaveLocalCopy(string sourcePath, string workoutTitle)
		{
			if (!_config.Format.Tcx || !_config.Format.SaveLocalCopy) return;

			_fileHandler.MkDirIfNotExists(_config.App.TcxDirectory);

			var backupDest = Path.Join(_config.App.TcxDirectory, $"{workoutTitle}.tcx");
			_fileHandler.Copy(sourcePath, backupDest, overwrite: true);
			_logger.Information("[{@Format}] Backed up file {@File}", FileFormat.Tcx, backupDest);
		}

		protected override XElement Convert(Workout workout, WorkoutSamples samples, UserData userDat)
		{
			using var tracing = Tracing.Trace($"{nameof(TcxConverter)}.{nameof(Convert)}")
								.WithTag(TagKey.Format, FileFormat.Tcx.ToString())
								.WithWorkoutId(workout.Id);

			XNamespace ns1 = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			XNamespace activityExtensions = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";
			XNamespace trackPointExtensions = "http://www.garmin.com/xmlschemas/TrackPointExtension/v2";
			XNamespace profileExtension = "http://www.garmin.com/xmlschemas/ProfileExtension/v1";
			XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

			var sport = GetSport(workout);
			var subSport = GetSubSport(workout);
			var startTime = GetStartTimeUtc(workout);

			var outputSummary = GetOutputSummary(samples);
			var hrSummary = GetHeartRateSummary(samples);
			var cadenceSummary = GetCadenceSummary(samples);
			var resistanceSummary = GetResistanceSummary(samples);
			var deviceInfo = GetDeviceInfo(workout.Fitness_Discipline);

			var lx = new XElement(activityExtensions + "TPX");
			lx.Add(new XElement(activityExtensions + "TotalPower", workout?.Total_Work));
			lx.Add(new XElement(activityExtensions + "MaximumCadence", cadenceSummary?.Max_Value));
			lx.Add(new XElement(activityExtensions + "AverageCadence", cadenceSummary?.Average_Value));
			lx.Add(new XElement(activityExtensions + "AverageWatts", outputSummary?.Average_Value));
			lx.Add(new XElement(activityExtensions + "MaximumWatts", outputSummary?.Max_Value));
			lx.Add(new XElement(activityExtensions + "AverageResistance", resistanceSummary?.Average_Value));
			lx.Add(new XElement(activityExtensions + "MaximumResistance", resistanceSummary?.Max_Value));

			var extensions = new XElement(ns1 + "Extensions");
			extensions.Add(lx);

			var track = new XElement(ns1+"Track");
			var allMetrics = samples.Metrics;
			var hrMetrics = allMetrics.FirstOrDefault(m => m.Slug == "heart_rate");
			var outputMetrics = allMetrics.FirstOrDefault(m => m.Slug == "output");
			var cadenceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "cadence");
			var speedMetrics = GetSpeedSummary(samples);
			var resistanceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "resistance");
			var locationMetrics = samples.Location_Data?.SelectMany(x => x.Coordinates).ToArray();
			var altitudeMetrics = allMetrics.FirstOrDefault(m => m.Slug == "altitude");
			for (var i = 0; i < samples.Seconds_Since_Pedaling_Start.Count; i++)
			{
				var trackPoint = new XElement(ns1+"TrackPoint");

				if (locationMetrics is object && i < locationMetrics.Length)
				{
					var trackPosition = new XElement(ns1+"Position");
					trackPosition.Add(new XElement(ns1+"LatitudeDegrees", locationMetrics[i].Latitude));
					trackPosition.Add(new XElement(ns1+"LongitudeDegrees", locationMetrics[i].Longitude));

					if (altitudeMetrics is object && i < altitudeMetrics.Values.Length)
						trackPosition.Add(new XElement(ns1+"AltitudeMeters", ConvertDistanceToMeters(altitudeMetrics.GetValue(i), altitudeMetrics.Display_Unit)));

					trackPoint.Add(trackPosition);
				}

				if (hrMetrics is object && i < hrMetrics.Values.Length)
				{
					var hr = new XElement(ns1+"HeartRateBpm");
					hr.Add(new XElement(ns1+"Value", hrMetrics.Values[i]));
					trackPoint.Add(hr);
				}

				if (cadenceMetrics is object && i < cadenceMetrics.Values.Length)
					trackPoint.Add(new XElement(ns1+"Cadence", cadenceMetrics.Values[i]));

				var tpx = new XElement(activityExtensions + "TPX");
				if (speedMetrics is object && i < speedMetrics.Values.Length)
					tpx.Add(new XElement(activityExtensions + "Speed", ConvertToMetersPerSecond(speedMetrics.GetValue(i), samples)));

				if (outputMetrics is object && i < outputMetrics.Values.Length)
					tpx.Add(new XElement(activityExtensions + "Watts", outputMetrics.Values[i]));

				if (resistanceMetrics is object && i < resistanceMetrics.Values.Length)
					tpx.Add(new XElement(activityExtensions + "Resistance", resistanceMetrics.Values[i]));

				var trackPointExtension = new XElement(ns1+"Extensions");
				trackPointExtension.Add(tpx);

				trackPoint.Add(trackPointExtension);
				trackPoint.Add(new XElement(ns1+"Time", GetTimeStamp(startTime, i)));

				track.Add(trackPoint);
			}

			var lap = new XElement(ns1+"Lap");
			lap.SetAttributeValue("StartTime", GetTimeStamp(startTime));
			lap.Add(new XElement(ns1+"TotalTimeSeconds", samples.Duration));
			lap.Add(new XElement(ns1+"Intensity", "Active"));
			lap.Add(new XElement(ns1+"Triggermethod", "Manual"));
			lap.Add(new XElement(ns1+"DistanceMeters", GetTotalDistance(samples)));
			lap.Add(new XElement(ns1+"MaximumSpeed", GetMaxSpeedMetersPerSecond(samples)));
			lap.Add(new XElement(ns1+"Calories", GetCalorieSummary(samples)?.Value));
			lap.Add(new XElement(ns1+"AverageHeartRateBpm", new XElement(ns1+"Value", hrSummary?.Average_Value)));
			lap.Add(new XElement(ns1+"MaximumHeartRateBpm", new XElement(ns1+"Value", hrSummary?.Max_Value)));
			lap.Add(lx);
			lap.Add(track);

			var activity = new XElement(ns1 + "Activity");
			activity.SetAttributeValue("Sport", sport);
			activity.Add(new XElement(ns1+"Id", GetTimeStamp(startTime)));
			activity.Add(lap);

			var creatorVersion = new XElement(ns1+"Version");
			creatorVersion.Add(new XElement(ns1+"VersionMajor", deviceInfo.Version.VersionMajor));
			creatorVersion.Add(new XElement(ns1+"VersionMinor", deviceInfo.Version.VersionMinor));
			creatorVersion.Add(new XElement(ns1+"BuildMajor", deviceInfo.Version.BuildMajor));
			creatorVersion.Add(new XElement(ns1+"BuildMinor", deviceInfo.Version.BuildMinor));

			var creator = new XElement(ns1+"Creator");
			creator.Add(new XAttribute(xsi + "type", "Device_t"));
			creator.Add(new XElement(ns1+"Name", deviceInfo.Name));
			creator.Add(new XElement(ns1+"UnitId", deviceInfo.UnitId));
			creator.Add(new XElement(ns1+"ProductID", deviceInfo.ProductID));
			creator.Add(creatorVersion);
			activity.Add(creator);

			var activities = new XElement(ns1 + "Activities");
			activities.Add(activity);
			var root = new XElement(ns1 + "TrainingCenterDatabase",
			 						new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
									new XAttribute(XNamespace.Xmlns + nameof(activityExtensions), activityExtensions.NamespaceName),
									new XAttribute(XNamespace.Xmlns + nameof(trackPointExtensions), trackPointExtensions.NamespaceName),
									new XAttribute(XNamespace.Xmlns + nameof(profileExtension), profileExtension.NamespaceName));

			root.Add(activities);

			return root;
		}

		private string GetSport(Workout workout)
		{
			switch (workout.Fitness_Discipline)
			{
				case FitnessDiscipline.Cycling:
				case FitnessDiscipline.Bike_Bootcamp:
					return "Biking";
				case FitnessDiscipline.Running:
					return "treadmill_running";
				case FitnessDiscipline.Walking:
					return "Running";
				case FitnessDiscipline.Cardio:
				case FitnessDiscipline.Circuit:
				case FitnessDiscipline.Strength:
				case FitnessDiscipline.Stretching:
				case FitnessDiscipline.Yoga:
				case FitnessDiscipline.Meditation:
				default:
					return "Other";
			}
		}

		private string GetSubSport(Workout workout)
		{
			switch (workout.Fitness_Discipline)
			{
				case FitnessDiscipline.Cycling:
				case FitnessDiscipline.Bike_Bootcamp:
					return "indoor_cycling";
				case FitnessDiscipline.Running:
					return "treadmill_running";
				case FitnessDiscipline.Walking:
					return "walking";
				case FitnessDiscipline.Cardio:
				case FitnessDiscipline.Circuit:
				case FitnessDiscipline.Stretching:
					return "indoor_cardio";
				case FitnessDiscipline.Strength:
					return "strength_training";
				case FitnessDiscipline.Yoga:
					return "yoga";
				case FitnessDiscipline.Meditation:
					return "breathwork";
				default:
					return "Other";
			}
		}
	}
}
