////////////////////////////////////////////////////////////////////////////////
// The following FIT Protocol software provided may be used with FIT protocol
// devices only and remains the copyrighted property of Garmin Canada Inc.
// The software is being provided on an "as-is" basis and as an accommodation,
// and therefore all warranties, representations, or guarantees of any kind
// (whether express, implied or statutory) including, without limitation,
// warranties of merchantability, non-infringement, or fitness for a particular
// purpose, are specifically disclaimed.
//
// Copyright 2020 Garmin International, Inc.
////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using Dynastream.Fit;

namespace ActivityEncode
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateTimeBasedActivity();
            CreateLapSwimActivity();
        }
        static public void CreateTimeBasedActivity()
        {
            const double TwoPI = Math.PI * 2.0;
            const double SemicirclesPerMeter = 107.173;
            const string FileName = "ActivityEncodeRecipe.fit";

            var messages = new List<Mesg>();

            // The starting timestamp for the activity
            var startTime = new Dynastream.Fit.DateTime(System.DateTime.UtcNow);

            // Timer Events are a BEST PRACTICE for FIT ACTIVITY files
            var eventMesgStart = new EventMesg();
            eventMesgStart.SetTimestamp(startTime);
            eventMesgStart.SetEvent(Event.Timer);
            eventMesgStart.SetEventType(EventType.Start);
            messages.Add(eventMesgStart);

            // Create the Developer Id message for the developer data fields.
            var developerIdMesg = new DeveloperDataIdMesg();
            // It is a BEST PRACTICE to reuse the same Guid for all FIT files created by your platform
            byte[] appId = new Guid("00010203-0405-0607-0809-0A0B0C0D0E0F").ToByteArray();
            for (int i = 0; i < appId.Length; i++)
            {
                developerIdMesg.SetApplicationId(i, appId[i]);
            }
            developerIdMesg.SetDeveloperDataIndex(0);
            developerIdMesg.SetApplicationVersion(110);
            messages.Add(developerIdMesg);

            // Create the Developer Data Field Descriptions
            var doughnutsFieldDescMesg = new FieldDescriptionMesg();
            doughnutsFieldDescMesg.SetDeveloperDataIndex(0);
            doughnutsFieldDescMesg.SetFieldDefinitionNumber(0);
            doughnutsFieldDescMesg.SetFitBaseTypeId(FitBaseType.Float32);
            doughnutsFieldDescMesg.SetFieldName(0, "Doughnuts Earned");
            doughnutsFieldDescMesg.SetUnits(0, "doughnuts");
            doughnutsFieldDescMesg.SetNativeMesgNum(MesgNum.Session);
            messages.Add(doughnutsFieldDescMesg);

            FieldDescriptionMesg hrFieldDescMesg = new FieldDescriptionMesg();
            hrFieldDescMesg.SetDeveloperDataIndex(0);
            hrFieldDescMesg.SetFieldDefinitionNumber(1);
            hrFieldDescMesg.SetFitBaseTypeId(FitBaseType.Uint8);
            hrFieldDescMesg.SetFieldName(0, "Heart Rate");
            hrFieldDescMesg.SetUnits(0, "bpm");
            hrFieldDescMesg.SetNativeFieldNum(RecordMesg.FieldDefNum.HeartRate);
            hrFieldDescMesg.SetNativeMesgNum(MesgNum.Record);
            messages.Add(hrFieldDescMesg);

            // Every FIT ACTIVITY file MUST contain Record messages
            var timestamp = new Dynastream.Fit.DateTime(startTime);

            // Create one hour (3600 seconds) of Record data
            for (uint i = 0; i <= 3600; i++)
            {
                // Create a new Record message and set the timestamp
                var recordMesg = new RecordMesg();
                recordMesg.SetTimestamp(timestamp);

                // Fake Record Data of Various Signal Patterns
                recordMesg.SetDistance(i); // Ramp
                recordMesg.SetSpeed(1); // Flatline
                recordMesg.SetHeartRate((byte)((Math.Sin(TwoPI * (0.01 * i + 10)) + 1.0) * 127.0)); // Sine
                recordMesg.SetCadence((byte)(i % 255)); // Sawtooth
                recordMesg.SetPower((ushort)((i % 255) < 127 ? 150 : 250)); // Square
                recordMesg.SetAltitude((float)Math.Abs(((double)i % 255.0) - 127.0)); // Triangle
                recordMesg.SetPositionLat(0);
                recordMesg.SetPositionLong((int)Math.Round(i * SemicirclesPerMeter));

                // Add a Developer Field to the Record Message
                var hrDevField = new DeveloperField(hrFieldDescMesg, developerIdMesg);
                recordMesg.SetDeveloperField(hrDevField);
                hrDevField.SetValue((byte)((Math.Sin(TwoPI * (0.01 * i + 10)) + 1.0) * 127.0)); // Sine

                // Write the Rercord message to the output stream
                messages.Add(recordMesg);

                // Increment the timestamp by one second
                timestamp.Add(1);
            }

            // Timer Events are a BEST PRACTICE for FIT ACTIVITY files
            var eventMesgStop = new EventMesg();
            eventMesgStop.SetTimestamp(timestamp);
            eventMesgStop.SetEvent(Event.Timer);
            eventMesgStop.SetEventType(EventType.StopAll);
            messages.Add(eventMesgStop);

            // Every FIT ACTIVITY file MUST contain at least one Lap message
            var lapMesg = new LapMesg();
            lapMesg.SetMessageIndex(0);
            lapMesg.SetTimestamp(timestamp);
            lapMesg.SetStartTime(startTime);
            lapMesg.SetTotalElapsedTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());
            lapMesg.SetTotalTimerTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());
            messages.Add(lapMesg);

            // Every FIT ACTIVITY file MUST contain at least one Session message
            var sessionMesg = new SessionMesg();
            sessionMesg.SetMessageIndex(0);
            sessionMesg.SetTimestamp(timestamp);
            sessionMesg.SetStartTime(startTime);
            sessionMesg.SetTotalElapsedTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());
            sessionMesg.SetTotalTimerTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());
            sessionMesg.SetSport(Sport.StandUpPaddleboarding);
            sessionMesg.SetSubSport(SubSport.Generic);
            sessionMesg.SetFirstLapIndex(0);
            sessionMesg.SetNumLaps(1);

            // Add a Developer Field to the Session message
            var doughnutsEarnedDevField = new DeveloperField(doughnutsFieldDescMesg, developerIdMesg);
            doughnutsEarnedDevField.SetValue(sessionMesg.GetTotalElapsedTime() / 1200.0f);
            sessionMesg.SetDeveloperField(doughnutsEarnedDevField);
            messages.Add(sessionMesg);

            // Every FIT ACTIVITY file MUST contain EXACTLY one Activity message
            var activityMesg = new ActivityMesg();
            activityMesg.SetTimestamp(timestamp);
            activityMesg.SetNumSessions(1);
            var timezoneOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
            activityMesg.SetLocalTimestamp((uint)((int)timestamp.GetTimeStamp() + timezoneOffset));
            activityMesg.SetTotalTimerTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());
            messages.Add(activityMesg);

            CreateActivityFile(messages, FileName, startTime);

        }

        static public void CreateLapSwimActivity()
        {
            // Example Swim Data representing a 500 yard pool swim using different strokes and drills.
            var swimData = new List<Dictionary<string, object>>()
            {
                new Dictionary<string, object>(){{"type", "Active"},{"duration",20U},{"stroke","Freestyle"},{"strokes",30U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",25U},{"stroke","Freestyle"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",30U},{"stroke","Freestyle"},{"strokes",10U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",35U},{"stroke","Freestyle"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Idle"},{"duration",60U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",20U},{"stroke","Backstroke"},{"strokes",30U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",25U},{"stroke","Backstroke"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",30U},{"stroke","Backstroke"},{"strokes",10U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",35U},{"stroke","Backstroke"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Idle"},{"duration",60U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",20U},{"stroke","Breaststroke"},{"strokes",30U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",25U},{"stroke","Breaststroke"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",30U},{"stroke","Breaststroke"},{"strokes",10U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",35U},{"stroke","Breaststroke"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Idle"},{"duration",60U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",20U},{"stroke","Butterfly"},{"strokes",30U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",25U},{"stroke","Butterfly"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",30U},{"stroke","Butterfly"},{"strokes",10U}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",35U},{"stroke","Butterfly"},{"strokes",20U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Idle"},{"duration",60U}},
                new Dictionary<string, object>(){{"type", "Lap"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",40U},{"stroke","Drill"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",40U},{"stroke","Drill"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",40U},{"stroke","Drill"}},
                new Dictionary<string, object>(){{"type", "Active"},{"duration",40U},{"stroke","Drill"}},
                new Dictionary<string, object>(){{"type", "Lap"}},
            };

            const string FileName = "ActivityEncodeRecipeLapSwim.fit";
            var messages = new List<Mesg>();

            // The starting timestamp for the activity
            var startTime = new Dynastream.Fit.DateTime(System.DateTime.UtcNow);


            // Timer Events are a BEST PRACTICE for FIT ACTIVITY files
            var eventMesgStart = new EventMesg();
            eventMesgStart.SetTimestamp(startTime);
            eventMesgStart.SetEvent(Event.Timer);
            eventMesgStart.SetEventType(EventType.Start);
            messages.Add(eventMesgStart);

            //
            // Create a Length or Lap message for each item in the sample swim data. Calculate
            // distance, duration, and stroke count for each lap and the overall session.
            //

            // Session Accumulators
            uint sessionTotalElapsedTime = 0;
            float sessionDistance = 0;
            ushort sessionNumLengths = 0;
            ushort sessionNumActiveLengths = 0;
            ushort sessionTotalStrokes = 0;
            ushort sessionNumLaps = 0;

            // Lap accumulators
            uint lapTotalElapsedTime = 0;
            float lapDistance = 0;
            ushort lapNumActiveLengths = 0;
            ushort lapNumLengths = 0;
            ushort lapFirstLengthIndex = 0;
            ushort lapTotalStrokes = 0;
            var lapStartTime = new Dynastream.Fit.DateTime(startTime);

            var poolLength = 22.86f;
            var poolLengthUnit = DisplayMeasure.Statute;
            var timestamp = new Dynastream.Fit.DateTime(startTime);
            ushort messageIndex = 0;

            foreach (var swimLength in swimData)
            {
                string type = (string)swimLength["type"];

                if (type.Equals("Lap"))
                {
                    // Create a Lap message, set its fields, and write it to the file
                    var lapMesg = new LapMesg();
                    lapMesg.SetMessageIndex(sessionNumLaps);
                    lapMesg.SetTimestamp(timestamp);
                    lapMesg.SetStartTime(lapStartTime);
                    lapMesg.SetTotalElapsedTime(lapTotalElapsedTime);
                    lapMesg.SetTotalTimerTime(lapTotalElapsedTime);
                    lapMesg.SetTotalDistance(lapDistance);
                    lapMesg.SetFirstLengthIndex(lapFirstLengthIndex);
                    lapMesg.SetNumActiveLengths(lapNumActiveLengths);
                    lapMesg.SetNumLengths(lapNumLengths);
                    lapMesg.SetTotalStrokes(lapTotalStrokes);
                    lapMesg.SetAvgStrokeDistance(lapDistance / lapTotalStrokes);
                    lapMesg.SetSport(Sport.Swimming);
                    lapMesg.SetSubSport(SubSport.LapSwimming);
                    messages.Add(lapMesg);

                    sessionNumLaps++;

                    // Reset the Lap accumulators
                    lapFirstLengthIndex = messageIndex;
                    lapNumActiveLengths = 0;
                    lapNumLengths = 0;
                    lapTotalElapsedTime = 0;
                    lapDistance = 0;
                    lapTotalStrokes = 0;
                    lapStartTime = new Dynastream.Fit.DateTime(timestamp);
                }
                else
                {
                    uint duration = (uint)swimLength["duration"];
                    var lengthType = (LengthType)Enum.Parse(typeof(LengthType), type);

                    // Create a Length message and its fields
                    var lengthMesg = new LengthMesg();
                    lengthMesg.SetMessageIndex(messageIndex++);
                    lengthMesg.SetStartTime(timestamp);
                    lengthMesg.SetTotalElapsedTime(duration);
                    lengthMesg.SetTotalTimerTime(duration);
                    lengthMesg.SetLengthType(lengthType);

                    timestamp.Add(duration);
                    lengthMesg.SetTimestamp(timestamp);

                    // Create the Record message that pairs with the Length Message
                    var recordMesg = new RecordMesg();
                    recordMesg.SetTimestamp(timestamp);
                    recordMesg.SetDistance(sessionDistance + poolLength);

                    // Is this an Active Length?
                    if (lengthType == LengthType.Active)
                    {
                        // Get the Active data from the model
                        string stroke = swimLength.ContainsKey("stroke") ? (String)swimLength["stroke"] : "Freestyle";
                        uint strokes = swimLength.ContainsKey("strokes") ? (uint)swimLength["strokes"] : 0;
                        SwimStroke swimStroke = (SwimStroke)Enum.Parse(typeof(SwimStroke), stroke);

                        // Set the Active data on the Length Message
                        lengthMesg.SetAvgSpeed(poolLength / (float)duration);
                        lengthMesg.SetSwimStroke(swimStroke);

                        if (strokes > 0)
                        {
                            lengthMesg.SetTotalStrokes((ushort)strokes);
                            lengthMesg.SetAvgSwimmingCadence((byte)(strokes * 60U / duration));
                        }

                        // Set the Active data on the Record Message
                        recordMesg.SetSpeed(poolLength / (float)duration);
                        if (strokes > 0)
                        {
                            recordMesg.SetCadence((byte)((strokes * 60U) / duration));
                        }

                        // Increment the "Active" accumulators
                        sessionNumActiveLengths++;
                        lapNumActiveLengths++;
                        sessionDistance += poolLength;
                        lapDistance += poolLength;
                        sessionTotalStrokes += (ushort)strokes;
                        lapTotalStrokes += (ushort)strokes;
                    }

                    // Write the messages to the file
                    messages.Add(recordMesg);
                    messages.Add(lengthMesg);

                    // Increment the "Total" accumulators
                    sessionTotalElapsedTime += duration;
                    lapTotalElapsedTime += duration;
                    sessionNumLengths++;
                    lapNumLengths++;
                }
            }

            // Timer Events are a BEST PRACTICE for FIT ACTIVITY files
            var eventMesgStop = new EventMesg();
            eventMesgStop.SetTimestamp(timestamp);
            eventMesgStop.SetEvent(Event.Timer);
            eventMesgStop.SetEventType(EventType.StopAll);
            messages.Add(eventMesgStop);

            // Every FIT ACTIVITY file MUST contain at least one Session message
            var sessionMesg = new SessionMesg();
            sessionMesg.SetMessageIndex(0);
            sessionMesg.SetTimestamp(timestamp);
            sessionMesg.SetStartTime(startTime);
            sessionMesg.SetTotalElapsedTime(sessionTotalElapsedTime);
            sessionMesg.SetTotalTimerTime(sessionTotalElapsedTime);
            sessionMesg.SetTotalDistance(sessionDistance);
            sessionMesg.SetSport(Sport.Swimming);
            sessionMesg.SetSubSport(SubSport.LapSwimming);
            sessionMesg.SetFirstLapIndex(0);
            sessionMesg.SetNumLaps(sessionNumLaps);
            sessionMesg.SetPoolLength(poolLength);
            sessionMesg.SetPoolLengthUnit(poolLengthUnit);
            sessionMesg.SetNumLengths(sessionNumLengths);
            sessionMesg.SetNumActiveLengths(sessionNumActiveLengths);
            sessionMesg.SetTotalStrokes(sessionTotalStrokes);
            sessionMesg.SetAvgStrokeDistance(sessionDistance / sessionTotalStrokes);
            messages.Add(sessionMesg);

            // Every FIT ACTIVITY file MUST contain EXACTLY one Activity message
            var activityMesg = new ActivityMesg();
            activityMesg.SetTimestamp(timestamp);
            activityMesg.SetNumSessions(1);
            var timezoneOffset = (int)TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds;
            activityMesg.SetLocalTimestamp((uint)((int)timestamp.GetTimeStamp() + timezoneOffset));
            activityMesg.SetTotalTimerTime(sessionTotalElapsedTime);
            messages.Add(activityMesg);

            CreateActivityFile(messages, FileName, startTime);
        }

        static void CreateActivityFile(List<Mesg> messages, String filename, Dynastream.Fit.DateTime startTime)
        {
            // The combination of file type, manufacturer id, product id, and serial number should be unique.
            // When available, a non-random serial number should be used.
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
            FileStream fitDest = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

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
        }
    }
}
