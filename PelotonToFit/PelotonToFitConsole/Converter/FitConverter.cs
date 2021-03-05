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
		public ConversionDetails Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary)
		{
			var messages = new List<Mesg>();

			// Start Time
			var startTime = new Dynastream.Fit.DateTime(GetStartTime(workout));
			var eventMsgStart = new EventMesg();
			eventMsgStart.SetTimestamp(startTime);
			eventMsgStart.SetEvent(Event.Timer);
			eventMsgStart.SetEventType(EventType.Start);
			messages.Add(eventMsgStart);

			// Developer Data Fields
			var developerIdMesg = new DeveloperDataIdMesg();
			byte[] appId = new Guid().ToByteArray();
			Console.WriteLine(appId);
			for (int i = 0; i < appId.Length; i++)
			{
				developerIdMesg.SetApplicationId(i, appId[i]);
			}
			developerIdMesg.SetDeveloperDataIndex(0);
			developerIdMesg.SetApplicationVersion(110);
			messages.Add(developerIdMesg);

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

			if (workoutSamples.Segment_List.Any())
			{
				var totalElapsedTime = 0;
				foreach (var segment in workoutSamples.Segment_List)
				{
					var lapStartTime = new Dynastream.Fit.DateTime(startTime);
					lapStartTime.Add(segment.Start_Time_Offset);

					var lapEndTime = new Dynastream.Fit.DateTime(lapStartTime);
					lapEndTime.Add(segment.Length);

					var lapMesg = new LapMesg();
					lapMesg.SetTimestamp(lapStartTime);
					lapMesg.SetStartTime(lapStartTime);
					lapMesg.SetTotalElapsedTime(totalElapsedTime);
					lapMesg.SetTotalTimerTime(totalElapsedTime);
					messages.Add(lapMesg);

					totalElapsedTime += segment.Length;
				}
			}

			var sessionMesg = new SessionMesg();
			sessionMesg.SetTimestamp(startTime);
			sessionMesg.SetStartTime(startTime);
			sessionMesg.SetTotalElapsedTime(recordsTimeStamp.GetTimeStamp() - startTime.GetTimeStamp());
			sessionMesg.SetTotalTimerTime(recordsTimeStamp.GetTimeStamp() - startTime.GetTimeStamp());
			sessionMesg.SetSport(Sport.Cycling);
			sessionMesg.SetSubSport(SubSport.GravelCycling);
			sessionMesg.SetFirstLapIndex(0);
			sessionMesg.SetNumLaps((ushort)workoutSamples.Segment_List.Count());

			var activityMesg = new ActivityMesg();
			activityMesg.SetTimestamp(recordsTimeStamp);
			activityMesg.SetNumSessions(1);
			var timezoneOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
			activityMesg.SetLocalTimestamp((uint)((int)recordsTimeStamp.GetTimeStamp() + timezoneOffset));
			messages.Add(activityMesg);

			Dynastream.Fit.File fileType = Dynastream.Fit.File.Activity;
			ushort manufacturerId = Manufacturer.Development;
			ushort productId = 0;
			float softwareVersion = 1.0f;

			Random random = new Random();
			uint serialNumber = (uint)random.Next();

			// Every FIT file MUST contain a File ID message
			var fileIdMesg = new FileIdMesg();
			fileIdMesg.SetType(fileType);
			fileIdMesg.SetManufacturer(manufacturerId);
			fileIdMesg.SetProduct(productId);
			fileIdMesg.SetTimeCreated(startTime);
			fileIdMesg.SetSerialNumber(serialNumber);

			// A Device Info message is a BEST PRACTICE for FIT ACTIVITY files
			var deviceInfoMesg = new DeviceInfoMesg();
			deviceInfoMesg.SetDeviceIndex(DeviceIndex.Creator);
			deviceInfoMesg.SetManufacturer(Manufacturer.Development);
			deviceInfoMesg.SetProduct(productId);
			deviceInfoMesg.SetProductName("FIT Cookbook"); // Max 20 Chars
			deviceInfoMesg.SetSerialNumber(serialNumber);
			deviceInfoMesg.SetSoftwareVersion(softwareVersion);
			deviceInfoMesg.SetTimestamp(startTime);

			// Create the output stream, this can be any type of stream, including a file or memory stream. Must have read/write access
			FileStream fitDest = new FileStream($"./MyTest{serialNumber}.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

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

		public System.DateTime GetStartTime(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dtDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToUniversalTime();
			return dtDateTime;
		}
	}
}
