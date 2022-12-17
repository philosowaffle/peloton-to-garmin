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

namespace CourseEncode
{
    class Program
    {
        public const ushort ProductId = 0;

        static void Main(string[] args)
        {
            EncodeCourse();
        }

        public static void EncodeCourse()
        {
            const string filename = "CourseEncodeRecipe.fit";

            // Example Record Data Defining a Course
            var courseData = new List<Dictionary<string, object>>()
            {
                new Dictionary<string, object>(){{"timestamp",961262849U},{"position_lat",463583114},{"position_long",-1131028903},{"altitude",329f},{"distance",0f},{"speed",0f}},
                new Dictionary<string, object>(){{"timestamp",961262855U},{"position_lat",463583127},{"position_long",-1131031938},{"altitude",328.6f},{"distance",22.03f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262869U},{"position_lat",463583152},{"position_long",-1131038159},{"altitude",327.6f},{"distance",67.29f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262876U},{"position_lat",463583164},{"position_long",-1131041346},{"altitude",327f},{"distance",90.52f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262876U},{"position_lat",463583164},{"position_long",-1131041319},{"altitude",327f},{"distance",90.72f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262891U},{"position_lat",463588537},{"position_long",-1131041383},{"altitude",327f},{"distance",140.72f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262891U},{"position_lat",463588549},{"position_long",-1131041383},{"altitude",327f},{"distance",140.82f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262897U},{"position_lat",463588537},{"position_long",-1131038293},{"altitude",327.6f},{"distance",163.26f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262911U},{"position_lat",463588512},{"position_long",-1131032041},{"altitude",328.4f},{"distance",208.75f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262918U},{"position_lat",463588499},{"position_long",-1131028879},{"altitude",329f},{"distance",231.8f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262918U},{"position_lat",463588499},{"position_long",-1131028903},{"altitude",329f},{"distance",231.97f},{"speed",3.0f}},
                new Dictionary<string, object>(){{"timestamp",961262933U},{"position_lat",463583127},{"position_long",-1131028903},{"altitude",329f},{"distance",281.96f},{"speed",3.0f}},
            };

            // Create the output stream, this can be any type of stream, including a file or memory stream. Must have read/write access.
            FileStream fitDest = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Create a FIT Encode object
            Encode encoder = new Encode(ProtocolVersion.V10);

            // Write the FIT header to the output stream
            encoder.Open(fitDest);

            // Reference points for the course
            var firstRecord = courseData[0];
            var lastRecord = courseData[courseData.Count - 1];
            var halfwayRecord = courseData[courseData.Count / 2];
            var startTimestamp = (uint)firstRecord["timestamp"];
            var endTimestamp = (uint)lastRecord["timestamp"];
            var startDateTime = new Dynastream.Fit.DateTime(startTimestamp);
            var endDateTime = new Dynastream.Fit.DateTime(endTimestamp);

            // Every FIT file MUST contain a File ID message
            var fileIdMesg = new FileIdMesg();
            fileIdMesg.SetType(Dynastream.Fit.File.Course);
            fileIdMesg.SetManufacturer(Manufacturer.Development);
            fileIdMesg.SetProduct(ProductId);
            fileIdMesg.SetTimeCreated(startDateTime);
            fileIdMesg.SetSerialNumber(startDateTime.GetTimeStamp());
            encoder.Write(fileIdMesg);

            // Every FIT file MUST contain a Course message
            var courseMesg = new CourseMesg();
            courseMesg.SetName("Garmin Field Day");
            courseMesg.SetSport(Sport.Cycling);
            encoder.Write(courseMesg);

            // Every FIT COURSE file MUST contain a Lap message
            var lapMesg = new LapMesg();
            lapMesg.SetStartTime(startDateTime);
            lapMesg.SetTimestamp(startDateTime);
            lapMesg.SetTotalElapsedTime(endTimestamp - startTimestamp);
            lapMesg.SetTotalTimerTime(endTimestamp - startTimestamp);
            lapMesg.SetStartPositionLat((int)firstRecord["position_lat"]);
            lapMesg.SetStartPositionLong((int)firstRecord["position_long"]);
            lapMesg.SetEndPositionLat((int)lastRecord["position_lat"]);
            lapMesg.SetEndPositionLong((int)lastRecord["position_long"]);
            lapMesg.SetTotalDistance((float)lastRecord["distance"]);
            encoder.Write(lapMesg);

            // Timer Events are REQUIRED for FIT COURSE files
            var eventMesgStart = new EventMesg();
            eventMesgStart.SetTimestamp(startDateTime);
            eventMesgStart.SetEvent(Event.Timer);
            eventMesgStart.SetEventType(EventType.Start);
            encoder.Write(eventMesgStart);

            // Every FIT COURSE file MUST contain Record messages
            foreach (var record in courseData)
            {
                var timestamp = (uint)record["timestamp"];
                var latitude = (int)record["position_lat"];
                var longitude = (int)record["position_long"];
                var distance = (float)record["distance"];
                var speed = (float)record["speed"];
                var altitude = (float)record["altitude"];

                var recordMesg = new RecordMesg();
                recordMesg.SetTimestamp(new Dynastream.Fit.DateTime(timestamp));
                recordMesg.SetPositionLat(latitude);
                recordMesg.SetPositionLong(longitude);
                recordMesg.SetDistance(distance);
                recordMesg.SetSpeed(speed);
                recordMesg.SetAltitude(altitude);
                encoder.Write(recordMesg);

                // Add a Course Point at the halfway point of the route
                if (record == halfwayRecord)
                {
                    var coursePointMesg = new CoursePointMesg();
                    coursePointMesg.SetTimestamp(new Dynastream.Fit.DateTime(timestamp));
                    coursePointMesg.SetName("Halfway");
                    coursePointMesg.SetType(CoursePoint.Generic);
                    coursePointMesg.SetPositionLat(latitude);
                    coursePointMesg.SetPositionLong(longitude);
                    coursePointMesg.SetDistance(distance);
                    encoder.Write(coursePointMesg);
                }
            }

            // Timer Events are REQUIRED for FIT COURSE files
            var eventMesgStop = new EventMesg();
            eventMesgStop.SetTimestamp(endDateTime);
            eventMesgStop.SetEvent(Event.Timer);
            eventMesgStop.SetEventType(EventType.StopAll);
            encoder.Write(eventMesgStop);

            // Update the data size in the header and calculate the CRC
            encoder.Close();

            // Close the output stream
            fitDest.Close();

            Console.WriteLine($"Encoded FIT file {fitDest.Name}");
        }
    }
}
