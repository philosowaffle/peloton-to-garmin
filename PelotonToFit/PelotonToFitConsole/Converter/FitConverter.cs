using Dynastream.Fit;
using Peloton.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PelotonToFitConsole.Converter
{
	public class FitConverter : IConverter
	{
		private static readonly float _softwareVersion = 1.0f;
		private static readonly ushort _productId = 0;
		private static readonly ushort _manufacturerId = Manufacturer.Development;
		private static readonly uint _serialNumber = 1234098765;
		private static readonly float _metersPerMile = 1609.34f;

		public ConversionDetails Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary, Configuration config)
		{
			var output = new ConversionDetails();

			var messages = new List<Mesg>();

			var startTime = GetStartTime(workout);
			var endTime = new Dynastream.Fit.DateTime(startTime);
			endTime.Add(workoutSamples.Duration);
			var title = workout.Ride.Title.Replace(" ", "_");
			var sport = GetGarminSport(workout);
			var subSport = GetGarminSubSport(workout);

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

			if (stepsAndLaps.Values.Any())
			{
				var workoutMesg = new WorkoutMesg();
				workoutMesg.SetCapabilities(32);
				workoutMesg.SetSport(sport);
				workoutMesg.SetSubSport(subSport);
				workoutMesg.SetWktName(title.Replace("_"," "));
				workoutMesg.SetNumValidSteps((ushort)stepsAndLaps.Keys.Count);
				messages.Add(workoutMesg);

				// add steps in order
				foreach (var tuple in stepsAndLaps.Values)
					messages.Add(tuple.Item1);

				// Add laps in order
				foreach (var tuple in stepsAndLaps.Values)
					messages.Add(tuple.Item2);
			}

			messages.Add(GetSessionMesg(workout, workoutSamples, workoutSummary, startTime, endTime, (ushort)stepsAndLaps.Keys.Count));			

			var activityMesg = new ActivityMesg();
			activityMesg.SetTimestamp(endTime);
			activityMesg.SetTotalTimerTime(workoutSamples.Duration);
			activityMesg.SetNumSessions(1);
			activityMesg.SetType(Activity.Manual);
			activityMesg.SetEvent(Event.Activity);
			activityMesg.SetEventType(EventType.Stop);
			
			messages.Add(activityMesg);

			using (FileStream fitDest = new FileStream(Path.Join(config.Application.FitDirectory, $"{title}.fit"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
			{
				Encode encoder = new Encode(ProtocolVersion.V20);
				encoder.Open(fitDest);
				foreach (Mesg message in messages)
				{
					encoder.Write(message);
				}
				encoder.Close();

				if (config.Application.DebugSeverity == Severity.Info)
					Console.WriteLine($"Encoded FIT file {fitDest.Name}");

				output.Path = fitDest.Name;
			}
			return output;
		}

		public void Decode(string filePath)
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
			Console.Out.WriteLine($"{e.mesg.Name}::");
			foreach (var f in e.mesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}::{f.GetValue()}");
			}
		}

		private static void WriteLap(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as LapMesg;

			Console.Out.WriteLine("LAP::");
			Console.Out.WriteLine($"{lapmesg.GetWktStepIndex()}");
			foreach (var f in lapmesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}:{f.GetValue()}");
			}
		}

		private static void WriteWorkout(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as WorkoutMesg;

			Console.Out.WriteLine("WORKOUT::");
			foreach (var f in lapmesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}:{f.GetValue()}");
			}
		}

		private static void WriteWorkoutStep(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as WorkoutStepMesg;

			Console.Out.WriteLine("WORKOUTSTEP::");
			foreach (var f in lapmesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}:{f.GetValue()}");
			}
		}

		private Dynastream.Fit.DateTime GetStartTime(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dtDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToUniversalTime();
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
					messages.Add(record);
					recordsTimeStamp.Add(1);
				}
			}

			return recordsTimeStamp;
		}

		private Sport GetGarminSport(Workout workout)
		{
			var fitnessDiscipline = workout.Fitness_Discipline;
			switch (fitnessDiscipline)
			{
				case "cycling":
					return Sport.Cycling;
				case "running":
					return Sport.Running;
				case "walking":
					return Sport.Walking;
				case "cardio":
				case "strength":
				case "yoga":
				default:
					return Sport.All;
			}
		}

		private SubSport GetGarminSubSport(Workout workout)
		{
			var fitnessDiscipline = workout.Fitness_Discipline;
			switch (fitnessDiscipline)
			{
				case "cycling":
					return SubSport.IndoorCycling;
				case "running":
					return SubSport.IndoorRunning;
				case "walking":
					return SubSport.IndoorWalking;
				case "cardio":
					return SubSport.CardioTraining;
				case "strength":
					return SubSport.StrengthTraining;
				case "yoga":
					return SubSport.Yoga;
				default:
					return SubSport.All;
			}
		}

		private float ConvertDistanceToMeters(double value, string unit)
		{
			switch (unit)
			{
				case "km":
					return (float)value * 1000;
				case "mi":
					return (float)value * _metersPerMile;
				default:
					return (float)value;
			}
		}

		private float GetTotalDistance(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
				return 0.0f;

			var unit = distanceSummary.Display_Unit;
			return ConvertDistanceToMeters(distanceSummary.Value, unit);
		}

		private float ConvertToMetersPerSecond(double value, WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
				return (float)value;

			var unit = distanceSummary.Display_Unit;
			var metersPerHour = ConvertDistanceToMeters(value, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;

			return metersPerSecond;
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

			uint previousCadenceLower = 0;
			uint previousCadenceUpper = 0;
			ushort stepIndex = 0;
			var duration = 0;
			WorkoutStepMesg workoutStep = null;
			LapMesg lapMesg = null;

			foreach (var secondSinceStart in workoutSamples.Seconds_Since_Pedaling_Start)
			{
				var index = secondSinceStart - 1;
				duration++;

				var currentCadenceLower = (uint)cadenceTargets.Lower[index];
				var currentCadenceUpper = (uint)cadenceTargets.Upper[index];

				if (currentCadenceLower != previousCadenceLower
					|| currentCadenceUpper != previousCadenceUpper)
				{
					if (workoutStep != null && lapMesg != null)
					{
						Console.Out.Write(duration);
						workoutStep.SetDurationValue((uint)duration * 1000); // milliseconds

						var lapEndTime = new Dynastream.Fit.DateTime(startTime);
						lapEndTime.Add(secondSinceStart);
						lapMesg.SetTotalElapsedTime(duration);
						lapMesg.SetTotalTimerTime(duration);
						lapMesg.SetTimestamp(lapEndTime);
						lapMesg.SetEventType(EventType.Stop);

						stepsAndLaps.Add(stepIndex, new Tuple<WorkoutStepMesg, LapMesg>(workoutStep, lapMesg));
						stepIndex++;
						duration = 0;
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
