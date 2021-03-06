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

		public ConversionDetails Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary)
		{
			var messages = new List<Mesg>();

			var startTime = GetStartTime(workout);

			messages.Add(GetStartTimeMesg(startTime));
			var endTime = AddMetrics(messages, workoutSamples, startTime);
			messages.Add(GetEndTimeMesg(endTime));
			AddLaps(messages, workoutSamples, startTime);

			var sessionMesg = new SessionMesg();
			sessionMesg.SetTimestamp(endTime);
			sessionMesg.SetStartTime(startTime);
			var totalTime = workoutSamples.Duration;
			sessionMesg.SetTotalElapsedTime(totalTime);
			sessionMesg.SetTotalTimerTime(totalTime);

			sessionMesg.SetSport(GetGarminSport(workout));
			sessionMesg.SetSubSport(GetGarminSubSport(workout));
			sessionMesg.SetFirstLapIndex(0);
			sessionMesg.SetNumLaps((ushort)workoutSamples.Segment_List.Count());
			sessionMesg.SetTotalCalories((ushort)workoutSummary.Calories);
			sessionMesg.SetTotalWork((uint)workoutSummary.Total_Work);
			sessionMesg.SetTotalDistance(GetTotalDistance(workoutSamples));

			// HR zones
			if (workoutSamples.Metrics.Any())
			{
				var hrZones = workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "heart_rate").Zones;
				var hrz1 = hrZones.FirstOrDefault(z => z.Slug == "zone1");
				sessionMesg.SetTimeInHrZone(1, hrz1.Duration);

				var hrz2 = hrZones.FirstOrDefault(z => z.Slug == "zone2");
				sessionMesg.SetTimeInHrZone(2, hrz2.Duration);

				var hrz3 = hrZones.FirstOrDefault(z => z.Slug == "zone3");
				sessionMesg.SetTimeInHrZone(3, hrz3.Duration);

				var hrz4 = hrZones.FirstOrDefault(z => z.Slug == "zone4");
				sessionMesg.SetTimeInHrZone(4, hrz4.Duration);

				var hrz5 = hrZones.FirstOrDefault(z => z.Slug == "zone5");
				sessionMesg.SetTimeInHrZone(5, hrz5.Duration);
			}

			messages.Add(sessionMesg);

			var stepCounter = 0;
			var cadenceTarget = workoutSamples.Target_Performance_Metrics?.Target_Graph_Metrics?.FirstOrDefault(t => t.Type == "cadence");
			if (cadenceTarget != null)
			{
				var previousUpper = 0;
				var previousLower = 0;
				var duration = 0f;
				WorkoutStepMesg previousStepMesg = null;
				for(var i = 0; i < cadenceTarget.Graph_Data.Upper.Count(); i++)
				{
					duration++;
					var currentUpper = cadenceTarget.Graph_Data.Upper.ElementAt(i);
					var currentLower = cadenceTarget.Graph_Data.Lower.ElementAt(i);
					if (previousUpper != currentUpper || previousLower != currentLower)
					{
						if (previousStepMesg != null)
						{
							stepCounter++;
							previousStepMesg.SetDurationTime(duration);
							messages.Add(previousStepMesg);
						}
						
						previousStepMesg = new WorkoutStepMesg();
						previousStepMesg.SetCustomTargetCadenceHigh((uint)currentUpper);
						previousStepMesg.SetTargetType(WktStepTarget.Cadence);
						previousStepMesg.SetDurationType(WktStepDuration.Time);
						previousStepMesg.SetCustomTargetCadenceLow((uint)currentLower);
						previousLower = currentLower;
						previousUpper = currentUpper;
						duration = 0;
					}

				}
			}

			if (stepCounter > 0)
			{
				var workoutMesg = new WorkoutMesg();
				workoutMesg.SetNumValidSteps((ushort)stepCounter);
				messages.Add(workoutMesg);
			}

			var zoneTargetMesg = new ZonesTargetMesg();
			zoneTargetMesg.SetFunctionalThresholdPower((ushort)workout.Ftp_Info.Ftp);
			messages.Add(zoneTargetMesg);

			var activityMesg = new ActivityMesg();
			activityMesg.SetTimestamp(endTime);
			activityMesg.SetNumSessions(1);
			var timezoneOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
			activityMesg.SetLocalTimestamp((uint)((int)endTime.GetTimeStamp() + timezoneOffset));
			messages.Add(activityMesg);

			// Every FIT file MUST contain a File ID message
			var fileIdMesg = new FileIdMesg();
			fileIdMesg.SetType(Dynastream.Fit.File.Activity);
			fileIdMesg.SetManufacturer(_manufacturerId);
			fileIdMesg.SetProduct(_productId);
			fileIdMesg.SetTimeCreated(startTime);
			fileIdMesg.SetSerialNumber(_serialNumber);

			// A Device Info message is a BEST PRACTICE for FIT ACTIVITY files
			var deviceInfoMesg = new DeviceInfoMesg();
			deviceInfoMesg.SetDeviceIndex(DeviceIndex.Creator);
			deviceInfoMesg.SetManufacturer(Manufacturer.Development);
			deviceInfoMesg.SetProduct(_productId);
			deviceInfoMesg.SetProductName("PelotonToGarmin"); // Max 20 Chars
			deviceInfoMesg.SetSerialNumber(_serialNumber);
			deviceInfoMesg.SetSoftwareVersion(_softwareVersion);
			deviceInfoMesg.SetTimestamp(startTime);

			// Create the output stream, this can be any type of stream, including a file or memory stream. Must have read/write access
			FileStream fitDest = new FileStream($"./{workout.Name}.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

			// Create a FIT Encode object
			Encode encoder = new Encode(ProtocolVersion.V20);

			// Write the FIT header to the output stream
			encoder.Open(fitDest);

			// Write the messages to the file, in the proper sequence
			encoder.Write(fileIdMesg);
			encoder.Write(deviceInfoMesg);

			foreach (Mesg message in messages)
			{
				encoder.Write(message);
			}

			// Update the data size in the header and calculate the CRC
			encoder.Close();

			// Close the output stream
			fitDest.Close();

			Console.WriteLine($"Encoded FIT file {fitDest.Name}");

			return new ConversionDetails();
		}

		private Dynastream.Fit.DateTime GetStartTime(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dtDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToUniversalTime();
			return new Dynastream.Fit.DateTime(dtDateTime);
		}

		private EventMesg GetStartTimeMesg(Dynastream.Fit.DateTime startTime)
		{
			var eventMsgStart = new EventMesg();
			eventMsgStart.SetTimestamp(startTime);
			eventMsgStart.SetEvent(Event.Timer);
			eventMsgStart.SetEventType(EventType.Start);
			return eventMsgStart;
		}

		private EventMesg GetEndTimeMesg(Dynastream.Fit.DateTime endTime)
		{
			var eventMesgStop = new EventMesg();
			eventMesgStop.SetTimestamp(endTime);
			eventMesgStop.SetEvent(Event.Timer);
			eventMesgStop.SetEventType(EventType.StopAll);
			return eventMesgStop;
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
						record.SetResistance((byte)resistanceMetrics.Values[i]);

					messages.Add(record);
					recordsTimeStamp.Add(1);
				}
			}

			return recordsTimeStamp;
		}

		private void AddLaps(ICollection<Mesg> messages, WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime)
		{
			if (workoutSamples.Segment_List.Any())
			{
				var totalElapsedTime = 0;
				foreach (var segment in workoutSamples.Segment_List)
				{
					var lapStartTime = new Dynastream.Fit.DateTime(startTime);
					lapStartTime.Add(segment.Start_Time_Offset);

					totalElapsedTime += segment.Length;

					var lapMesg = new LapMesg();
					lapMesg.SetStartTime(lapStartTime);
					lapMesg.SetTotalElapsedTime(segment.Length);
					lapMesg.SetTotalTimerTime(segment.Length);
					messages.Add(lapMesg);
				}
			}
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
	}
}
