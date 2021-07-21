using Common;
using Common.Database;
using Common.Dto;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Conversion
{
	public class TcxConverter : Converter<XElement>
	{
		public TcxConverter(Configuration config, IDbClient dbClient, IFileHandling fileHandler) : base(config, dbClient, fileHandler) { }

		public override void Convert()
		{
			if (!_config.Format.Tcx) return;

			base.Convert("tcx");
		}

		protected override void Save(XElement data, string path)
		{
			data.Save(path);
		}

		protected override XElement Convert(Workout workout, WorkoutSamples samples)
		{
			XNamespace ns1 = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			XNamespace activityExtensions = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";
			XNamespace trackPointExtensions = "http://www.garmin.com/xmlschemas/TrackPointExtension/v2";
			XNamespace profileExtension = "http://www.garmin.com/xmlschemas/ProfileExtension/v1";

			var sport = GetSport(workout);
			var subSport = GetSubSport(workout);
			var startTime = GetStartTimeUtc(workout);

			var outputSummary = GetOutputSummary(samples);
			var hrSummary = GetHeartRateSummary(samples);
			var cadenceSummary = GetCadenceSummary(samples);
			var resistanceSummary = GetResistanceSummary(samples);
			var deviceInfo = GetDeviceInfo();

			var lx = new XElement(activityExtensions + "TPX");
			lx.Add(new XElement(activityExtensions + "TotalPower", workout?.Total_Work));
			lx.Add(new XElement(activityExtensions + "MaximumCadence", cadenceSummary?.Max_Value));
			lx.Add(new XElement(activityExtensions + "AverageCadence", cadenceSummary?.Average_Value));
			lx.Add(new XElement(activityExtensions + "AverageWatts", outputSummary?.Average_Value));
			lx.Add(new XElement(activityExtensions + "MaximumWatts", outputSummary?.Max_Value));
			lx.Add(new XElement(activityExtensions + "AverageResistance", resistanceSummary?.Average_Value));
			lx.Add(new XElement(activityExtensions + "MaximumResistance", resistanceSummary?.Max_Value));

			var extensions = new XElement("Extensions");
			extensions.Add(lx);

			var track = new XElement("Track");
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
				var trackPoint = new XElement("TrackPoint");

				if (locationMetrics is object && i < locationMetrics.Length)
				{
					var trackPosition = new XElement("Position");
					trackPosition.Add(new XElement("LatitudeDegrees", locationMetrics[i].Latitude));
					trackPosition.Add(new XElement("LongitudeDegrees", locationMetrics[i].Longitude));

					if (altitudeMetrics is object && i < altitudeMetrics.Values.Length)
						trackPosition.Add(new XElement("AltitudeMeters", ConvertDistanceToMeters(altitudeMetrics.GetValue(i), altitudeMetrics.Display_Unit)));

					trackPoint.Add(trackPosition);
				}

				if (hrMetrics is object && i < hrMetrics.Values.Length)
				{
					var hr = new XElement("HeartRateBpm");
					hr.Add(new XElement("Value", hrMetrics.Values[i]));
					trackPoint.Add(hr);
				}

				if (cadenceMetrics is object && i < cadenceMetrics.Values.Length)
					trackPoint.Add(new XElement("Cadence", cadenceMetrics.Values[i]));

				var tpx = new XElement(activityExtensions + "TPX");
				if (speedMetrics is object && i < speedMetrics.Values.Length)
					tpx.Add(new XElement(activityExtensions + "Speed", ConvertToMetersPerSecond(speedMetrics.GetValue(i), samples)));

				if (outputMetrics is object && i < outputMetrics.Values.Length)
					tpx.Add(new XElement(activityExtensions + "Watts", outputMetrics.Values[i]));

				if (resistanceMetrics is object && i < resistanceMetrics.Values.Length)
					tpx.Add(new XElement(activityExtensions + "Resistance", resistanceMetrics.Values[i]));

				var trackPointExtension = new XElement("Extensions");
				trackPointExtension.Add(tpx);

				trackPoint.Add(trackPointExtension);
				trackPoint.Add(new XElement("Time", GetTimeStamp(startTime, i)));

				track.Add(trackPoint);
			}

			var lap = new XElement("Lap");
			lap.SetAttributeValue("StartTime", GetTimeStamp(startTime));
			lap.Add(new XElement("TotalTimeSeconds", workout.Ride.Duration));
			lap.Add(new XElement("Intensity", "Active"));
			lap.Add(new XElement("Triggermethod", "Manual"));
			lap.Add(new XElement("DistanceMeters", GetTotalDistance(samples)));
			lap.Add(new XElement("MaximumSpeed", GetMaxSpeedMetersPerSecond(samples)));
			lap.Add(new XElement("Calories", GetCalorieSummary(samples)?.Value));
			lap.Add(new XElement("AverageHeartRateBpm", new XElement("Value", hrSummary?.Average_Value)));
			lap.Add(new XElement("MaximumHeartRateBpm", new XElement("Value", hrSummary?.Max_Value)));
			lap.Add(lx);
			lap.Add(track);

			var activity = new XElement("Activity");
			activity.SetAttributeValue("Sport", sport);
			activity.Add(new XElement("Id", GetTimeStamp(startTime)));
			activity.Add(lap);

			var creatorVersion = new XElement("Version");
			creatorVersion.Add(new XElement("VersionMajor", deviceInfo.Version.VersionMajor));
			creatorVersion.Add(new XElement("VersionMinor", deviceInfo.Version.VersionMinor));
			creatorVersion.Add(new XElement("BuildMajor", deviceInfo.Version.BuildMajor));
			creatorVersion.Add(new XElement("BuildMinor", deviceInfo.Version.BuildMinor));

			var creator = new XElement("Creator");
			creator.Add(new XElement("Name", deviceInfo.Name));
			creator.Add(new XElement("UnitId", deviceInfo.UnitId));
			creator.Add(new XElement("ProductID", deviceInfo.ProductID));
			creator.Add(creatorVersion);
			activity.Add(creator);

			var activities = new XElement("Activities");
			activities.Add(activity);

			var root = new XElement("TrainingCenterDatabase",
									// new XAttribute("xsi:" + "schemaLocation", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd"),
									new XAttribute(XNamespace.Xmlns + nameof(ns1), ns1.NamespaceName),
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
				case "cycling":
				case "bike_bootcamp":
					return "Biking";
				case "running":
					return "treadmill_running";
				case "walking":
					return "Running";
				case "cardio":
				case "circuit":
				case "stretching":
				case "strength":
				case "yoga":
				case "meditation":
				default:
					return "Other";
			}
		}

		private string GetSubSport(Workout workout)
		{
			switch (workout.Fitness_Discipline)
			{
				case "cycling":
				case "bike_bootcamp":
					return "indoor_cycling";
				case "running":
					return "treadmill_running";
				case "walking":
					return "walking";
				case "cardio":
				case "circuit":
				case "stretching":
					return "indoor_cardio";
				case "strength":
					return "strength_training";
				case "yoga":
					return "yoga";
				case "meditation":
					return "breathwork";
				default:
					return "Other";
			}
		}

		public override void Decode(string filePath)
		{
			throw new NotImplementedException();
		}
	}
}
