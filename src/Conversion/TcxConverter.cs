using Common;
using Common.Database;
using Common.Dto;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Metrics = Prometheus.Metrics;

namespace Conversion
{
	public class TcxConverter : Converter
	{
		private static readonly Histogram WorkoutsConversionDuration = Metrics.CreateHistogram("p2g_tcx_workouts_conversion_duration_seconds", "Histogram of all workout conversion duration.");
		private static readonly Histogram WorkoutConversionDuration = Metrics.CreateHistogram("p2g_tcx_workout_conversion_duration_seconds", "Histogram of a workout conversion durations.");
		private static readonly Gauge WorkoutsToConvert = Metrics.CreateGauge("p2g_tcx_workout_conversion_pending", "The number of workouts pending conversion to output format.");
		private static readonly Counter WorkoutsConverted = Metrics.CreateCounter("p2g_tcx_workouts_converted_total", "The number of workouts converted.");

		private Configuration _config;
		private DbClient _dbClient;

		public TcxConverter(Configuration config, DbClient dbClient)
		{
			_config = config;
			_dbClient = dbClient;
		}

		// TODO: refactor some of this to abstract base since this logic is almost identical between converters
		public override void Convert()
		{
			if (!_config.Format.Tcx) return;

			if (!Directory.Exists(_config.App.DownloadDirectory))
			{
				Log.Information("No working directory found. Nothing to do.");
				return;
			}

			var files = Directory.GetFiles(_config.App.DownloadDirectory);

			if (files.Length == 0)
			{
				Log.Information("No files to convert in working directory. Nothing to do.");
				return;
			}

			FileHandling.MkDirIfNotEists(_config.App.TcxDirectory);

			var prepUpload = _config.Garmin.Upload && _config.Garmin.FormatToUpload == "tcx";
			if (prepUpload)
				FileHandling.MkDirIfNotEists(_config.App.UploadDirectory);

			WorkoutsToConvert.Set(files.Count());
			using var timer = WorkoutsConversionDuration.NewTimer();
			foreach (var file in files)
			{
				using var workoutTimer = WorkoutConversionDuration.NewTimer();

				// load file and deserialize
				P2GWorkout workoutData = null;
				try
				{
					using (var reader = new StreamReader(file))
					{
						workoutData = JsonSerializer.Deserialize<P2GWorkout>(reader.ReadToEnd(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
					}
				}
				catch (Exception e)
				{
					Log.Error(e, "Failed to load and parse workout data {@File}", file);
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
					WorkoutsToConvert.Dec();
					continue;
				}

				using var tracing = Tracing.Trace("Convert")
										.WithWorkoutId(workoutData.Workout.Id)
										.SetTag(TagKey.Format, TagValue.Tcx);

				// call internal convert method
				var converted = Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.WorkoutSummary);

				if (converted is null)
				{
					Log.Error("Failed to convert workout data {@File}", file);
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
					WorkoutsToConvert.Dec();
					continue;
				}

				// write to output dir
				var title = GetTitle(workoutData.Workout);
				var path = Path.Join(_config.App.WorkingDirectory, $"{title}.tcx");
				try
				{
					converted.Save(path);
				}
				catch (Exception e)
				{
					Log.Error(e, "Failed to write tcx file for {@File}", title);
					WorkoutsToConvert.Dec();
					continue;
				}

				// copy to local save
				if (_config.Format.SaveLocalCopy)
				{
					try
					{
						var backupDest = Path.Join(_config.App.TcxDirectory, $"{title}.tcx");
						File.Copy(path, backupDest, overwrite: true);
						Log.Information("Backed up TCX file {0}", backupDest);
					}
					catch (Exception e)
					{
						Log.Error(e, "Failed to copy tcx file for {@File}", title);
						continue;
					}
				}

				// copy to upload dir
				if (prepUpload)
				{
					try
					{
						var uploadDest = Path.Join(_config.App.UploadDirectory, $"{title}.tcx");
						File.Copy(path, uploadDest, overwrite: true);
						Log.Debug("Prepped TCX file {@Path} for upload.", uploadDest);
					}
					catch (Exception e)
					{
						Log.Error(e, "Failed to copy tcx file for {@File}", title);
						continue;
					}
				}

				// update db item with tcx conversion
				SyncHistoryItem syncRecord = _dbClient.Get(workoutData.Workout.Id);
				if (syncRecord?.DownloadDate is null)
				{
					var startTimeInSeconds = workoutData.Workout.Start_Time;
					var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToLocalTime();

					syncRecord = new SyncHistoryItem(workoutData.Workout)
					{
						DownloadDate = DateTime.Now
					};
				}

				syncRecord.ConvertedToTcx = true;
				_dbClient.Upsert(syncRecord);
				WorkoutsToConvert.Dec();
				WorkoutsConverted.Inc();
			}
		}

		private XElement Convert(Workout workout, WorkoutSamples samples, WorkoutSummary summary)
		{
			XNamespace ns1 = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			XNamespace activityExtensions = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";
			XNamespace trackPointExtensions = "http://www.garmin.com/xmlschemas/TrackPointExtension/v2";
			XNamespace profileExtension = "http://www.garmin.com/xmlschemas/ProfileExtension/v1";

			var sport = GetSport(workout);
			var subSport = GetSubSport(workout);
			var startTime = GetStartTime(workout);

			var lx = new XElement(activityExtensions + "TPX");
			lx.Add(new XElement(activityExtensions + "TotalPower", summary.Total_Work));
			lx.Add(new XElement(activityExtensions + "MaximumCadence", summary.Max_Cadence));
			lx.Add(new XElement(activityExtensions + "AverageCadence", summary.Avg_Cadence));
			lx.Add(new XElement(activityExtensions + "AverageWatts", summary.Avg_Power));
			lx.Add(new XElement(activityExtensions + "MaximumWatts", summary.Max_Power));
			lx.Add(new XElement(activityExtensions + "AverageResistance", summary.Avg_Resistance));
			lx.Add(new XElement(activityExtensions + "MaximumResistance", summary.Max_Resistance));

			var extensions = new XElement("Extensions");
			extensions.Add(lx);

			var track = new XElement("Track");
			var allMetrics = samples.Metrics;
			var hrMetrics = allMetrics.FirstOrDefault(m => m.Slug == "heart_rate");
			var outputMetrics = allMetrics.FirstOrDefault(m => m.Slug == "output");
			var cadenceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "cadence");
			var speedMetrics = allMetrics.FirstOrDefault(m => m.Slug == "speed");
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
						trackPosition.Add(new XElement("AltitudeMeters", ConvertDistanceToMeters(altitudeMetrics.Values[i], altitudeMetrics.Display_Unit)));

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
					tpx.Add(new XElement(activityExtensions + "Speed", ConvertToMetersPerSecond(speedMetrics.Values[i], samples)));

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
			lap.Add(new XElement("Calories", summary.Calories));
			lap.Add(new XElement("AverageHeartRateBpm", new XElement("Value", summary.Avg_Heart_Rate)));
			lap.Add(new XElement("MaximumHeartRateBpm", new XElement("Value", summary.Max_Heart_Rate)));
			lap.Add(lx);
			lap.Add(track);

			var activity = new XElement("Activity");
			activity.SetAttributeValue("Sport", sport);
			activity.Add(new XElement("Id", GetTimeStamp(startTime)));
			activity.Add(lap);

			var activities = new XElement("Activities");
			activities.Add(activity);

			var root = new XElement("TrainingCenterDatabase",
									//new XAttribute("xsi:" + "schemaLocation", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd"),
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
