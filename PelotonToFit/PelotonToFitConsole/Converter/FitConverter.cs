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
		private static readonly Guid _appId = new Guid("c955ba17-adb1-4042-a591-831a9bd35b60");
		private static readonly uint _appVersion = 0;
		private static readonly float _softwareVersion = 1.0f;
		private static readonly ushort _productId = 0;
		private static readonly ushort _manufacturerId = Manufacturer.Development;
		private static readonly uint _serialNumber = 1234098765;

		public ConversionDetails Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary)
		{
			var messages = new List<Mesg>();

			var startTime = GetStartTime(workout);

			// Start Time			
			messages.Add(GetStartTimeMesg(startTime));

			// Developer Data Fields			
			//messages.Add(GetDeveloperIdMesg());			

			var allMetrics = workoutSamples.Metrics;
			var hrMetrics = allMetrics.FirstOrDefault(m => m.Slug == "heart_rate");
			var outputMetrics = allMetrics.FirstOrDefault(m => m.Slug == "output");
			var cadenceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "cadence");
			var speedMetrics = allMetrics.FirstOrDefault(m => m.Slug == "speed");
			var resistanceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "resistance");

			var recordsTimeStamp = new Dynastream.Fit.DateTime(startTime);
			if (workoutSamples.Seconds_Since_Pedalling_Start is object)
			{
				for (var i = 0; i < workoutSamples.Seconds_Since_Pedalling_Start.Count; i++)
				{
					var record = new RecordMesg();
					record.SetTimestamp(recordsTimeStamp);

					record.SetDistance(i); // TODO: this is wrong
					record.SetSpeed((float)speedMetrics.Values[i]);
					record.SetHeartRate((byte)hrMetrics.Values[i]);
					record.SetCadence((byte)cadenceMetrics.Values[i]);
					record.SetPower((ushort)outputMetrics.Values[i]);
					record.SetResistance((byte)resistanceMetrics.Values[i]);

					messages.Add(record);
					recordsTimeStamp.Add(1);
				}
			}

			var eventMesgStop = new EventMesg();
			eventMesgStop.SetTimestamp(recordsTimeStamp);
			eventMesgStop.SetEvent(Event.Timer);
			eventMesgStop.SetEventType(EventType.StopAll);
			messages.Add(eventMesgStop);

			// Every FIT ACTIVITY file MUST contain at least one Lap message
			var lapMesg = new LapMesg();
			lapMesg.SetTimestamp(recordsTimeStamp);
			lapMesg.SetStartTime(startTime);
			lapMesg.SetTotalElapsedTime(recordsTimeStamp.GetTimeStamp() - startTime.GetTimeStamp());
			lapMesg.SetTotalTimerTime(recordsTimeStamp.GetTimeStamp() - startTime.GetTimeStamp());
			messages.Add(lapMesg);

			//if (workoutSamples.Segment_List.Any())
			//{
			//	var totalElapsedTime = 0;
			//	foreach (var segment in workoutSamples.Segment_List)
			//	{
			//		var lapStartTime = new Dynastream.Fit.DateTime(startTime);
			//		lapStartTime.Add(segment.Start_Time_Offset);

			//		var lapEndTime = new Dynastream.Fit.DateTime(lapStartTime);
			//		lapEndTime.Add(segment.Length);

			//		var lapMesg = new LapMesg();
			//		lapMesg.SetTimestamp(lapStartTime);
			//		lapMesg.SetStartTime(lapStartTime);
			//		lapMesg.SetTotalElapsedTime(totalElapsedTime);
			//		lapMesg.SetTotalTimerTime(totalElapsedTime);
			//		messages.Add(lapMesg);

			//		totalElapsedTime += segment.Length;
			//	}
			//}

			var sessionMesg = new SessionMesg();
			sessionMesg.SetTimestamp(recordsTimeStamp);
			sessionMesg.SetStartTime(startTime);
			sessionMesg.SetTotalElapsedTime(recordsTimeStamp.GetTimeStamp() - startTime.GetTimeStamp());
			sessionMesg.SetTotalTimerTime(recordsTimeStamp.GetTimeStamp() - startTime.GetTimeStamp());
			sessionMesg.SetSport(Sport.Cycling);
			sessionMesg.SetSubSport(SubSport.GravelCycling);
			sessionMesg.SetFirstLapIndex(0);
			sessionMesg.SetNumLaps((ushort)workoutSamples.Segment_List.Count());
			messages.Add(sessionMesg);

			var activityMesg = new ActivityMesg();
			activityMesg.SetTimestamp(recordsTimeStamp);
			activityMesg.SetNumSessions(1);
			var timezoneOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
			activityMesg.SetLocalTimestamp((uint)((int)recordsTimeStamp.GetTimeStamp() + timezoneOffset));
			messages.Add(activityMesg);			

			// A Device Info message is a BEST PRACTICE for FIT ACTIVITY files
			var deviceInfoMesg = new DeviceInfoMesg();
			deviceInfoMesg.SetDeviceIndex(DeviceIndex.Creator);
			deviceInfoMesg.SetManufacturer(Manufacturer.Development);
			deviceInfoMesg.SetProduct(_productId);
			deviceInfoMesg.SetProductName("FIT Cookbook"); // Max 20 Chars
			deviceInfoMesg.SetSerialNumber(_serialNumber);
			deviceInfoMesg.SetSoftwareVersion(_softwareVersion);
			deviceInfoMesg.SetTimestamp(startTime);

			// Every FIT file MUST contain a File ID message
			var fileIdMesg = new FileIdMesg();
			fileIdMesg.SetType(Dynastream.Fit.File.Activity);
			fileIdMesg.SetManufacturer(_manufacturerId);
			fileIdMesg.SetProduct(_productId);
			fileIdMesg.SetTimeCreated(startTime);
			fileIdMesg.SetSerialNumber(_serialNumber);

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

		private DeveloperDataIdMesg GetDeveloperIdMesg()
		{
			var developerIdMesg = new DeveloperDataIdMesg();
			byte[] appId = _appId.ToByteArray();
			for (int i = 0; i < appId.Length; i++)
			{
				developerIdMesg.SetApplicationId(i, appId[i]);
			}
			developerIdMesg.SetDeveloperDataIndex(0);
			developerIdMesg.SetApplicationVersion(_appVersion);
			return developerIdMesg;
		}
	}
}
