using Common;
using Common.Database;
using Common.Dto;
using Dynastream.Fit;
using Prometheus;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Metrics = Prometheus.Metrics;

namespace PelotonToFitConsole.Converter
{
	public class FitConverter : Converter
	{
		private static readonly float _softwareVersion = 1.0f;
		private static readonly ushort _productId = 0;
		private static readonly ushort _manufacturerId = Manufacturer.Development;
		private static readonly uint _serialNumber = 1234098765;
		
		private static readonly string _spaceSeparator = "_";

		private static readonly Histogram WorkoutsConversionDuration = Metrics.CreateHistogram("p2g_fit_workouts_conversion_duration_seconds", "Histogram of all workout conversion duration.");
		private static readonly Histogram WorkoutConversionDuration = Metrics.CreateHistogram("p2g_fit_workout_conversion_duration_seconds", "Histogram of a workout conversion durations.");
		private static readonly Gauge WorkoutsToConvert = Metrics.CreateGauge("p2g_fit_workout_conversion_pending", "The number of workouts pending conversion to output format.");
		private static readonly Counter WorkoutsConverted = Metrics.CreateCounter("p2g_fit_workouts_converted_total", "The number of workouts converted.");

		private Configuration _config;
		private DbClient _dbClient;

		public FitConverter(Configuration config, DbClient dbClient)
		{
			_config = config;
			_dbClient = dbClient;
		}

		public override void Convert()
		{
			if (!_config.Format.Fit) return;
			
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

			FileHandling.MkDirIfNotEists(_config.App.FitDirectory);

			var prepUpload = _config.Garmin.Upload && _config.Garmin.FormatToUpload == "fit";
			if (prepUpload)
				FileHandling.MkDirIfNotEists(_config.App.UploadDirectory);

			// Foreach file in directory
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
				} catch (Exception e)
				{
					Log.Error(e, "Failed to load and parse workout data {@File}", file);
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
					WorkoutsToConvert.Dec();
					continue;
				}

				using var tracing = Tracing.Trace("Convert")
										.WithWorkoutId(workoutData.Workout.Id)
										.SetTag(TagKey.Format, TagValue.Fit);

				// call internal convert method
				var converted = Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.WorkoutSummary);

				if (string.IsNullOrEmpty(converted.Item1))
				{
					Log.Error("Failed to convert workout data {@File}", file);
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
					WorkoutsToConvert.Dec();
					continue;
				}

				// write to output dir
				var path = Path.Join(_config.App.WorkingDirectory, $"{converted.Item1}.fit");
				try
				{
					using (FileStream fitDest = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
					{
						Encode encoder = new Encode(ProtocolVersion.V20);
						encoder.Open(fitDest);
						foreach (Mesg message in converted.Item2)
						{
							encoder.Write(message);
						}
						encoder.Close();

						Log.Information("Encoded FIT file {0}", fitDest.Name);
					}

				}
				catch (Exception e)
				{
					Log.Error(e, "Failed to write fit file for {@File}", converted.Item1);
					WorkoutsToConvert.Dec();
					continue;
				}

				// copy to local save
				if (_config.Format.SaveLocalCopy)
				{
					try
					{
						var backupDest = Path.Join(_config.App.FitDirectory, $"{converted.Item1}.fit");
						System.IO.File.Copy(path, backupDest, overwrite: true);
						Log.Information("Backed up FIT file {0}", backupDest);
					}
					catch (Exception e)
					{
						Log.Error(e, "Failed to copy fit file for {@File}", converted.Item1);
						continue;
					}
				}

				// copy to upload dir
				if (prepUpload)
				{
					try
					{
						var uploadDest = Path.Join(_config.App.UploadDirectory, $"{converted.Item1}.fit");
						System.IO.File.Copy(path, uploadDest, overwrite: true);
						Log.Debug("Prepped FIT file {@Path} for upload.", uploadDest);
					}
					catch (Exception e)
					{
						Log.Error(e, "Failed to copy fit file for {@File}", converted.Item1);
						continue;
					}
				}

				// update db item with fit conversion date
				SyncHistoryItem syncRecord = _dbClient.Get(workoutData.Workout.Id);
				if (syncRecord?.DownloadDate is null)
				{
					var startTimeInSeconds = workoutData.Workout.Start_Time;
					var dtDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToLocalTime();

					syncRecord = new SyncHistoryItem(workoutData.Workout)
					{
						DownloadDate = System.DateTime.Now
					};
				}

				syncRecord.ConvertedToFit = true;
				_dbClient.Upsert(syncRecord);
				WorkoutsToConvert.Dec();
				WorkoutsConverted.Inc();
			}
		}

		private Tuple<string, ICollection<Mesg>> Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary)
		{
			// MESSAGE ORDER MATTERS
			var messages = new List<Mesg>();

			var startTime = GetStartTime(workout);
			var endTime = new Dynastream.Fit.DateTime(startTime);
			endTime.Add(workoutSamples.Duration);
			var title = GetTitle(workout);
			var sport = GetGarminSport(workout);
			var subSport = GetGarminSubSport(workout);

			if (sport == Sport.Invalid)
			{
				Log.Error("Unsupported Sport Type - Skipping {@Sport}", workout.Fitness_Discipline);
				return new Tuple<string, ICollection<Mesg>>(string.Empty, null);
			}

			var fileIdMesg = new FileIdMesg();
			fileIdMesg.SetSerialNumber(_serialNumber);
			fileIdMesg.SetTimeCreated(startTime);
			fileIdMesg.SetManufacturer(_manufacturerId);
			fileIdMesg.SetProduct(_productId);
			fileIdMesg.SetType(Dynastream.Fit.File.Activity);
			messages.Add(fileIdMesg);

			var eventMesg = new EventMesg();
			eventMesg.SetTimestamp(startTime);
			eventMesg.SetData(0);
			eventMesg.SetEvent(Event.Timer);
			eventMesg.SetEventType(EventType.Start);
			eventMesg.SetEventGroup(0);
			messages.Add(eventMesg);

			var deviceInfoMesg = new DeviceInfoMesg();
			deviceInfoMesg.SetTimestamp(startTime);
			deviceInfoMesg.SetSerialNumber(_serialNumber);
			deviceInfoMesg.SetManufacturer(Manufacturer.Garmin);
			deviceInfoMesg.SetProduct(_productId);
			deviceInfoMesg.SetSoftwareVersion(_softwareVersion);
			deviceInfoMesg.SetProductName("PelotonToGarmin"); // Max 20 Chars
			messages.Add(deviceInfoMesg);

			var sportMesg = new SportMesg();
			sportMesg.SetSport(sport);
			sportMesg.SetSubSport(subSport);
			messages.Add(sportMesg);

			var zoneTargetMesg = new ZonesTargetMesg();
			zoneTargetMesg.SetFunctionalThresholdPower((ushort)workout.Ftp_Info.Ftp);
			zoneTargetMesg.SetPwrCalcType(PwrZoneCalc.PercentFtp);
			messages.Add(zoneTargetMesg);

			var trainingMesg = new TrainingFileMesg();
			trainingMesg.SetTimestamp(startTime);
			trainingMesg.SetTimeCreated(startTime);
			trainingMesg.SetSerialNumber(_serialNumber);
			trainingMesg.SetManufacturer(_manufacturerId);
			trainingMesg.SetProduct(_productId);
			trainingMesg.SetType(Dynastream.Fit.File.Workout);
			messages.Add(trainingMesg);

			AddMetrics(messages, workoutSamples, startTime);

			var stepsAndLaps = GetWorkoutStepsAndLaps(workoutSamples, startTime, sport, subSport);

			var workoutMesg = new WorkoutMesg();
			workoutMesg.SetWktName(title.Replace(_spaceSeparator, " "));
			workoutMesg.SetCapabilities(32);
			workoutMesg.SetSport(sport);
			workoutMesg.SetSubSport(subSport);
			workoutMesg.SetNumValidSteps((ushort)stepsAndLaps.Keys.Count);
			messages.Add(workoutMesg);

			// add steps in order
			foreach (var tuple in stepsAndLaps.Values)
				messages.Add(tuple.Item1);

			// Add laps in order
			foreach (var tuple in stepsAndLaps.Values)
				messages.Add(tuple.Item2);

			messages.Add(GetSessionMesg(workout, workoutSamples, workoutSummary, startTime, endTime, (ushort)stepsAndLaps.Keys.Count));

			var activityMesg = new ActivityMesg();
			activityMesg.SetTimestamp(endTime);
			activityMesg.SetTotalTimerTime(workoutSamples.Duration);
			activityMesg.SetNumSessions(1);
			activityMesg.SetType(Activity.Manual);
			activityMesg.SetEvent(Event.Activity);
			activityMesg.SetEventType(EventType.Stop);

			var timezoneOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
			activityMesg.SetLocalTimestamp((uint)((int)endTime.GetTimeStamp() + timezoneOffset));

			messages.Add(activityMesg);

			return new Tuple<string, ICollection<Mesg>>(title, messages);
		}

		public override void Decode(string filePath)
		{
			Decode decoder = new Decode();
			MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();

			decoder.MesgEvent += mesgBroadcaster.OnMesg;
			decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

			mesgBroadcaster.ActivityMesgEvent += Write;
			mesgBroadcaster.DeviceInfoMesgEvent += Write;
			mesgBroadcaster.EventMesgEvent += Write;
			mesgBroadcaster.FileIdMesgEvent += Write;
			mesgBroadcaster.LapMesgEvent += WriteLap;
			mesgBroadcaster.SegmentLapMesgEvent += Write;
			mesgBroadcaster.SessionMesgEvent += Write;
			mesgBroadcaster.UserProfileMesgEvent += Write;
			mesgBroadcaster.WorkoutMesgEvent += WriteWorkout;
			mesgBroadcaster.WorkoutStepMesgEvent += WriteWorkoutStep;
			mesgBroadcaster.ZonesTargetMesgEvent += Write;
			mesgBroadcaster.BikeProfileMesgEvent += Write;
			mesgBroadcaster.CadenceZoneMesgEvent += Write;
			mesgBroadcaster.DeveloperDataIdMesgEvent += Write;
			mesgBroadcaster.PowerZoneMesgEvent += Write;
			mesgBroadcaster.SportMesgEvent += Write;
			mesgBroadcaster.TrainingFileMesgEvent += Write;
			mesgBroadcaster.UserProfileMesgEvent += Write;
			mesgBroadcaster.WorkoutSessionMesgEvent += Write;
			//mesgBroadcaster.RecordMesgEvent += Write;

			FileStream fitDest = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			decoder.Read(fitDest);
		}
		private static void Write(object sender, MesgEventArgs e)
		{
			Log.Debug($"{e.mesg.Name}::");
			foreach (var f in e.mesg.Fields)
			{
				Log.Debug($"{f.Name}::{f.GetValue()}");
			}
		}

		private static void WriteLap(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as LapMesg;

			Log.Debug("LAP::");
			Log.Debug($"{lapmesg.GetWktStepIndex()}");
			foreach (var f in lapmesg.Fields)
			{
				Log.Debug($"{f.Name}:{f.GetValue()}");
			}
		}

		private static void WriteWorkout(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as WorkoutMesg;

			Log.Debug("WORKOUT::");
			foreach (var f in lapmesg.Fields)
			{
				Log.Debug($"{f.Name}:{f.GetValue()}");
			}
		}

		private static void WriteWorkoutStep(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as WorkoutStepMesg;

			Log.Debug("WORKOUTSTEP::");
			foreach (var f in lapmesg.Fields)
			{
				Log.Debug($"{f.Name}:{f.GetValue()}");
			}
		}

		private Dynastream.Fit.DateTime GetStartTime(Workout workout)
		{
			var dtDateTime = base.GetStartTime(workout);
			return new Dynastream.Fit.DateTime(dtDateTime);
		}

		private Dynastream.Fit.DateTime AddMetrics(ICollection<Mesg> messages, WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime)
		{
			var allMetrics = workoutSamples.Metrics;
			var hrMetrics = allMetrics.FirstOrDefault(m => m.Slug == "heart_rate");
			var outputMetrics = allMetrics.FirstOrDefault(m => m.Slug == "output");
			var cadenceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "cadence");
			var speedMetrics = allMetrics.FirstOrDefault(m => m.Slug == "speed");
			var resistanceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "resistance");
			var locationMetrics = workoutSamples.Location_Data?.SelectMany(x => x.Coordinates).ToArray();
			var altitudeMetrics = allMetrics.FirstOrDefault(m => m.Slug == "altitude");

			var recordsTimeStamp = new Dynastream.Fit.DateTime(startTime);
			if (workoutSamples.Seconds_Since_Pedaling_Start is object)
			{
				for (var i = 0; i < workoutSamples.Seconds_Since_Pedaling_Start.Count; i++)
				{
					var record = new RecordMesg();
					record.SetTimestamp(recordsTimeStamp);

					if (speedMetrics is object && i < speedMetrics.Values.Length)
						record.SetSpeed(ConvertToMetersPerSecond(speedMetrics.Values[i], workoutSamples));

					if (hrMetrics is object && i < hrMetrics.Values.Length)
						record.SetHeartRate((byte)hrMetrics.Values[i]);

					if (cadenceMetrics is object && i < cadenceMetrics.Values.Length)
						record.SetCadence((byte)cadenceMetrics.Values[i]);

					if (outputMetrics is object && i < outputMetrics.Values.Length)
						record.SetPower((ushort)outputMetrics.Values[i]);

					if (resistanceMetrics is object && i < resistanceMetrics.Values.Length)
					{
						var resistancePercent = resistanceMetrics.Values[i] / 1;
						record.SetResistance((byte)(254 * resistancePercent));
					}

					if (altitudeMetrics is object && i < altitudeMetrics.Values.Length)
					{
						var altitude = ConvertDistanceToMeters(altitudeMetrics.Values[i], altitudeMetrics.Display_Unit);						
						record.SetAltitude(altitude);
					}

					if (locationMetrics is object && i < locationMetrics.Length)
					{
						// unit is semicircles
						record.SetPositionLat(ConvertDegreesToSemicircles(locationMetrics[i].Latitude));
						record.SetPositionLong(ConvertDegreesToSemicircles(locationMetrics[i].Longitude));
					}

					messages.Add(record);
					recordsTimeStamp.Add(1);
				}
			}

			return recordsTimeStamp;
		}

		private int ConvertDegreesToSemicircles(float degrees)
		{
			return (int)(degrees * (Math.Pow(2, 31) / 180));
		}

		private Sport GetGarminSport(Workout workout)
		{
			var fitnessDiscipline = workout.Fitness_Discipline;
			switch (fitnessDiscipline)
			{
				case "cycling":
				case "bike_bootcamp":
					return Sport.Cycling;
				case "running":
					return Sport.Running;
				case "walking":
					return Sport.Walking;
				case "cardio":
				case "circuit":
				case "strength":
				case "stretching":
				case "yoga":
					return Sport.Training;
				default:
					return Sport.Invalid;
			}
		}

		private SubSport GetGarminSubSport(Workout workout)
		{
			var fitnessDiscipline = workout.Fitness_Discipline;
			switch (fitnessDiscipline)
			{
				case "cycling":
				case "bike_bootcamp":
					return SubSport.IndoorCycling;
				case "running":
					return SubSport.IndoorRunning;
				case "walking":
					return SubSport.IndoorWalking;
				case "cardio":
				case "circuit":
					return SubSport.CardioTraining;
				case "strength":
					return SubSport.StrengthTraining;
				case "yoga":
				case "stretching":
					return SubSport.Yoga;
				default:
					return SubSport.Generic;
			}
		}

		private SessionMesg GetSessionMesg(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary, Dynastream.Fit.DateTime startTime, Dynastream.Fit.DateTime endTime, ushort numLaps)
		{
			var sessionMesg = new SessionMesg();
			sessionMesg.SetTimestamp(endTime);
			sessionMesg.SetStartTime(startTime);
			var totalTime = workoutSamples.Duration;
			sessionMesg.SetTotalElapsedTime(totalTime);
			sessionMesg.SetTotalTimerTime(totalTime);
			sessionMesg.SetTotalDistance(GetTotalDistance(workoutSamples));
			sessionMesg.SetTotalWork((uint)workoutSummary.Total_Work);
			sessionMesg.SetTotalCalories((ushort)workoutSummary.Calories);
			sessionMesg.SetAvgPower((ushort)workoutSummary.Avg_Power);
			sessionMesg.SetMaxPower((ushort)workoutSummary.Max_Power);
			sessionMesg.SetFirstLapIndex(0);
			sessionMesg.SetNumLaps(numLaps);
			sessionMesg.SetThresholdPower((ushort)workout.Ftp_Info.Ftp);
			sessionMesg.SetEvent(Event.Lap);
			sessionMesg.SetEventType(EventType.Stop);
			sessionMesg.SetSport(GetGarminSport(workout));
			sessionMesg.SetSubSport(GetGarminSubSport(workout));
			sessionMesg.SetAvgHeartRate((byte)workoutSummary.Avg_Heart_Rate);
			sessionMesg.SetMaxHeartRate((byte)workoutSummary.Max_Heart_Rate);
			sessionMesg.SetAvgCadence((byte)workoutSummary.Avg_Cadence);
			sessionMesg.SetMaxCadence((byte)workoutSummary.Max_Cadence);
			sessionMesg.SetMaxSpeed(GetMaxSpeedMetersPerSecond(workoutSamples));
			sessionMesg.SetAvgSpeed(GetAvgSpeedMetersPerSecond(workoutSamples));
			
			// HR zones
			//if (workoutSamples.Metrics.Any())
			//{
			//	var hrZones = workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "heart_rate").Zones;
			//	var hrz1 = hrZones.FirstOrDefault(z => z.Slug == "zone1");
			//	sessionMesg.SetTimeInHrZone(1, hrz1.Duration);

			//	var hrz2 = hrZones.FirstOrDefault(z => z.Slug == "zone2");
			//	sessionMesg.SetTimeInHrZone(2, hrz2.Duration);

			//	var hrz3 = hrZones.FirstOrDefault(z => z.Slug == "zone3");
			//	sessionMesg.SetTimeInHrZone(3, hrz3.Duration);

			//	var hrz4 = hrZones.FirstOrDefault(z => z.Slug == "zone4");
			//	sessionMesg.SetTimeInHrZone(4, hrz4.Duration);

			//	var hrz5 = hrZones.FirstOrDefault(z => z.Slug == "zone5");
			//	sessionMesg.SetTimeInHrZone(5, hrz5.Duration);
			//}

			return sessionMesg;
		}

		private Dictionary<int, Tuple<WorkoutStepMesg, LapMesg>> GetWorkoutStepsAndLaps(WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime, Sport sport, SubSport subSport)
		{
			var stepsAndLaps = new Dictionary<int, Tuple<WorkoutStepMesg, LapMesg>>();

			if (workoutSamples is null
				|| workoutSamples.Target_Performance_Metrics is null
				|| workoutSamples.Target_Performance_Metrics.Target_Graph_Metrics is null)
				return stepsAndLaps;

			var cadenceTargets = workoutSamples.Target_Performance_Metrics.Target_Graph_Metrics.FirstOrDefault(w => w.Type == "cadence").Graph_Data;

			if (cadenceTargets is null)
				return stepsAndLaps;

			uint previousCadenceLower = 0;
			uint previousCadenceUpper = 0;
			ushort stepIndex = 0;
			var duration = 0;
			float lapDistanceInMeters = 0;
			WorkoutStepMesg workoutStep = null;
			LapMesg lapMesg = null;
			var speedMetrics = workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "speed");

			foreach (var secondSinceStart in workoutSamples.Seconds_Since_Pedaling_Start)
			{
				var index = secondSinceStart - 1;
				duration++;

				if (speedMetrics is object && index < speedMetrics.Values.Length)
				{
					var currentSpeedInMPS = ConvertToMetersPerSecond(speedMetrics.Values[index], workoutSamples);
					lapDistanceInMeters += 1 * currentSpeedInMPS;
				}

				var currentCadenceLower = (uint)cadenceTargets.Lower[index];
				var currentCadenceUpper = (uint)cadenceTargets.Upper[index];

				if (currentCadenceLower != previousCadenceLower
					|| currentCadenceUpper != previousCadenceUpper)
				{
					if (workoutStep != null && lapMesg != null)
					{
						workoutStep.SetDurationValue((uint)duration * 1000); // milliseconds

						var lapEndTime = new Dynastream.Fit.DateTime(startTime);
						lapEndTime.Add(secondSinceStart);
						lapMesg.SetTotalElapsedTime(duration);
						lapMesg.SetTotalTimerTime(duration);
						lapMesg.SetTimestamp(lapEndTime);
						lapMesg.SetEventType(EventType.Stop);
						lapMesg.SetTotalDistance(lapDistanceInMeters);

						stepsAndLaps.Add(stepIndex, new Tuple<WorkoutStepMesg, LapMesg>(workoutStep, lapMesg));
						stepIndex++;
						duration = 0;
						lapDistanceInMeters = 0;
					}

					workoutStep = new WorkoutStepMesg();
					workoutStep.SetDurationType(WktStepDuration.Time);
					workoutStep.SetMessageIndex(stepIndex);
					workoutStep.SetTargetType(WktStepTarget.Cadence);
					workoutStep.SetCustomTargetValueHigh(currentCadenceUpper);
					workoutStep.SetCustomTargetValueLow(currentCadenceLower);
					workoutStep.SetIntensity(currentCadenceUpper > 60 ? Intensity.Active : Intensity.Rest);

					lapMesg = new LapMesg();
					var lapStartTime = new Dynastream.Fit.DateTime(startTime);
					lapStartTime.Add(secondSinceStart);
					lapMesg.SetStartTime(lapStartTime);
					lapMesg.SetWktStepIndex(stepIndex);
					lapMesg.SetMessageIndex(stepIndex);
					lapMesg.SetEvent(Event.Lap);
					lapMesg.SetLapTrigger(LapTrigger.Time);
					lapMesg.SetSport(sport);
					lapMesg.SetSubSport(subSport);

					previousCadenceLower = currentCadenceLower;
					previousCadenceUpper = currentCadenceUpper;
				}
			}

			return stepsAndLaps;
		}

	}
}
