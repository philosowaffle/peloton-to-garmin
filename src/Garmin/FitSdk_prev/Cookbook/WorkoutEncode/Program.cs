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

namespace WorkoutEncode
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateBikeTempoWorkout();
            CreateRun800RepeatsWorkout();
            CreateCustomTargetValuesWorkout();
            CreatePoolSwimWorkout();
        }

        static void CreateBikeTempoWorkout()
        {
            var workoutSteps = new List<WorkoutStepMesg>();

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                       durationType: WktStepDuration.Time,
                                       durationValue: 600000, // milliseconds
                                       targetType: WktStepTarget.HeartRate,
                                       targetValue: 1,
                                       intensity: Intensity.Warmup));

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Time,
                                                durationValue: 2400000, // milliseconds
                                                targetType: WktStepTarget.Power,
                                                targetValue: 3));

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                intensity: Intensity.Cooldown));

            var workoutMesg = new WorkoutMesg();
            workoutMesg.SetWktName("Tempo Bike");
            workoutMesg.SetSport(Sport.Cycling);
            workoutMesg.SetSubSport(SubSport.Invalid);
            workoutMesg.SetNumValidSteps((ushort)workoutSteps.Count);

            CreateWorkout(workoutMesg, workoutSteps);
        }


        static void CreateRun800RepeatsWorkout()
        {
            var workoutSteps = new List<WorkoutStepMesg>();

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Distance,
                                                durationValue: 400000, // centimeters
                                                targetType: WktStepTarget.HeartRate,
                                                targetValue: 1,
                                                intensity: Intensity.Warmup));

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Distance,
                                                durationValue: 80000, // centimeters
                                                targetType: WktStepTarget.HeartRate,
                                                targetValue: 4));

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Distance,
                                                durationValue: 20000, // centimeters
                                                targetType: WktStepTarget.HeartRate,
                                                targetValue: 2,
                                                intensity: Intensity.Rest));

            workoutSteps.Add(CreateWorkoutStepRepeat(messageIndex: workoutSteps.Count, repeatFrom: 1, repetitions: 5));

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Distance,
                                                durationValue: 100000, // centimeters
                                                targetType: WktStepTarget.HeartRate,
                                                targetValue: 2,
                                                intensity: Intensity.Cooldown));

            var workoutMesg = new WorkoutMesg();
            workoutMesg.SetWktName("Running 800m Repeats");
            workoutMesg.SetSport(Sport.Running);
            workoutMesg.SetSubSport(SubSport.Invalid);
            workoutMesg.SetNumValidSteps((ushort)workoutSteps.Count);

            CreateWorkout(workoutMesg, workoutSteps);
        }

        static void CreateCustomTargetValuesWorkout()
        {
            var workoutSteps = new List<WorkoutStepMesg>();

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Time,
                                                durationValue: 600000, // milliseconds
                                                targetType: WktStepTarget.HeartRate,
                                                customTargetValueLow: 235, // 135 + 100
                                                customTargetValueHigh: 255, // 155 + 100
                                                intensity: Intensity.Warmup));

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Time,
                                                durationValue: 2400000, // milliseconds
                                                targetType: WktStepTarget.Power,
                                                customTargetValueLow: 1175, // 175 + 1000
                                                customTargetValueHigh: 1195)); // 195 + 1000

            workoutSteps.Add(CreateWorkoutStep(messageIndex: workoutSteps.Count,
                                                durationType: WktStepDuration.Time,
                                                durationValue: 600000, // milliseconds
                                                targetType: WktStepTarget.Speed,
                                                customTargetValueLow: 5556, // 5.556 meters/second * 1000
                                                customTargetValueHigh: 6944, // 6.944 meters/second * 1000
                                                intensity: Intensity.Cooldown));

            var workoutMesg = new WorkoutMesg();
            workoutMesg.SetWktName("Custom Target Values");
            workoutMesg.SetSport(Sport.Cycling);
            workoutMesg.SetSubSport(SubSport.Invalid);
            workoutMesg.SetNumValidSteps((ushort)workoutSteps.Count);

            CreateWorkout(workoutMesg, workoutSteps);
        }

        static void CreatePoolSwimWorkout()
        {
            var workoutSteps = new List<WorkoutStepMesg>();

            // Warm Up 200 yds
            workoutSteps.Add(CreateWorkoutStepSwim(messageIndex: workoutSteps.Count,
                                                    distance: 182.88f,
                                                    intensity: Intensity.Warmup));
            // Rest until lap button pressed
            workoutSteps.Add(CreateWorkoutStepSwimRest(messageIndex: workoutSteps.Count));

            // Drill w/ kickboard 200 yds
            workoutSteps.Add(CreateWorkoutStepSwim(messageIndex: workoutSteps.Count,
                                        distance: 182.88f,
                                        swimStroke: SwimStroke.Drill,
                                        equipment: WorkoutEquipment.SwimKickboard));
            // Rest until lap button pressed
            workoutSteps.Add(CreateWorkoutStepSwimRest(messageIndex: workoutSteps.Count));

            // 5 x 100 yds on 2:00
            workoutSteps.Add(CreateWorkoutStepSwim(messageIndex: workoutSteps.Count,
                                    distance: 91.44f,
                                    swimStroke: SwimStroke.Freestyle));

            workoutSteps.Add(CreateWorkoutStepSwimRest(messageIndex: workoutSteps.Count,
                                    durationType: WktStepDuration.RepetitionTime,
                                    durationTime: 120.0f));

            workoutSteps.Add(CreateWorkoutStepRepeat(messageIndex: workoutSteps.Count, repeatFrom: 4, repetitions: 5));

            // Rest until lap button pressed
            workoutSteps.Add(CreateWorkoutStepSwimRest(messageIndex: workoutSteps.Count));

            // Cool Down 100 yds
            workoutSteps.Add(CreateWorkoutStepSwim(messageIndex: workoutSteps.Count,
                                                distance: 91.44f,
                                                intensity: Intensity.Cooldown));

            var workoutMesg = new WorkoutMesg();
            workoutMesg.SetWktName("Pool Swim");
            workoutMesg.SetSport(Sport.Swimming);
            workoutMesg.SetSubSport(SubSport.LapSwimming);
            workoutMesg.SetPoolLength(22.86f); // 25 yards
            workoutMesg.SetPoolLengthUnit(DisplayMeasure.Statute);
            workoutMesg.SetNumValidSteps((ushort)workoutSteps.Count);

            CreateWorkout(workoutMesg, workoutSteps);
        }

        static void CreateWorkout(WorkoutMesg workoutMesg, List<WorkoutStepMesg> workoutSteps)
        {
            // The combination of file type, manufacturer id, product id, and serial number should be unique.
            // When available, a non-random serial number should be used.
            Dynastream.Fit.File fileType = Dynastream.Fit.File.Workout;
            ushort manufacturerId = Manufacturer.Development;
            ushort productId = 0;
            Random random = new Random();
            uint serialNumber = (uint)random.Next();

            // Every FIT file MUST contain a File ID message
            var fileIdMesg = new FileIdMesg();
            fileIdMesg.SetType(fileType);
            fileIdMesg.SetManufacturer(manufacturerId);
            fileIdMesg.SetProduct(productId);
            fileIdMesg.SetTimeCreated(new Dynastream.Fit.DateTime(System.DateTime.UtcNow));
            fileIdMesg.SetSerialNumber(serialNumber);

            // Create the output stream, this can be any type of stream, including a file or memory stream. Must have read/write access
            FileStream fitDest = new FileStream($"{workoutMesg.GetWktNameAsString().Replace(' ', '_')}.fit", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

            // Create a FIT Encode object
            Encode encoder = new Encode(ProtocolVersion.V10);

            // Write the FIT header to the output stream
            encoder.Open(fitDest);

            // Write the messages to the file, in the proper sequence
            encoder.Write(fileIdMesg);
            encoder.Write(workoutMesg);

            foreach (WorkoutStepMesg workoutStep in workoutSteps)
            {
                encoder.Write(workoutStep);
            }

            // Update the data size in the header and calculate the CRC
            encoder.Close();

            // Close the output stream
            fitDest.Close();

            Console.WriteLine($"Encoded FIT file {fitDest.Name}");
        }

        private static WorkoutStepMesg CreateWorkoutStep(int messageIndex, String name = null, String notes = null, Intensity intensity = Intensity.Active, WktStepDuration durationType = WktStepDuration.Open, uint? durationValue = null, WktStepTarget targetType = WktStepTarget.Open, uint targetValue = 0, uint? customTargetValueLow = null, uint? customTargetValueHigh = null)
        {
            if (durationType == WktStepDuration.Invalid)
            {
                return null;
            }

            var workoutStepMesg = new WorkoutStepMesg();
            workoutStepMesg.SetMessageIndex((ushort)messageIndex);

            if (name != null)
            {
                workoutStepMesg.SetWktStepName(name);
            }

            if (notes != null)
            {
                workoutStepMesg.SetNotes(notes);
            }

            workoutStepMesg.SetIntensity(intensity);
            workoutStepMesg.SetDurationType(durationType);

            if (durationValue.HasValue)
            {
                workoutStepMesg.SetDurationValue(durationValue);
            }

            if (targetType != WktStepTarget.Invalid && customTargetValueLow.HasValue && customTargetValueHigh.HasValue)
            {
                workoutStepMesg.SetTargetType(targetType);
                workoutStepMesg.SetTargetValue(0);
                workoutStepMesg.SetCustomTargetValueLow(customTargetValueLow);
                workoutStepMesg.SetCustomTargetValueHigh(customTargetValueHigh);
            }
            else if (targetType != WktStepTarget.Invalid)
            {
                workoutStepMesg.SetTargetType(targetType);
                workoutStepMesg.SetTargetValue(targetValue);
                workoutStepMesg.SetCustomTargetValueLow(0);
                workoutStepMesg.SetCustomTargetValueHigh(0);
            }

            return workoutStepMesg;
        }

        private static WorkoutStepMesg CreateWorkoutStepRepeat(int messageIndex, uint repeatFrom, uint repetitions)
        {
            var workoutStepMesg = new WorkoutStepMesg();
            workoutStepMesg.SetMessageIndex((ushort)messageIndex);

            workoutStepMesg.SetDurationType(WktStepDuration.RepeatUntilStepsCmplt);
            workoutStepMesg.SetDurationValue(repeatFrom);

            workoutStepMesg.SetTargetType(WktStepTarget.Open);
            workoutStepMesg.SetTargetValue(repetitions);

            return workoutStepMesg;
        }

        private static WorkoutStepMesg CreateWorkoutStepSwim(int messageIndex, float distance, String name = null, String notes = null, Intensity intensity = Intensity.Active, SwimStroke swimStroke = SwimStroke.Invalid, WorkoutEquipment? equipment = null)
        {
            var workoutStepMesg = new WorkoutStepMesg();
            workoutStepMesg.SetMessageIndex((ushort)messageIndex);

            if (name != null)
            {
                workoutStepMesg.SetWktStepName(name);
            }

            if (notes != null)
            {
                workoutStepMesg.SetNotes(notes);
            }

            workoutStepMesg.SetIntensity(intensity);

            workoutStepMesg.SetDurationType(WktStepDuration.Distance);
            workoutStepMesg.SetDurationDistance(distance);

            workoutStepMesg.SetTargetType(WktStepTarget.SwimStroke);

            workoutStepMesg.SetTargetStrokeType((byte)swimStroke);

            if (equipment.HasValue)
            {
                workoutStepMesg.SetEquipment(equipment);
            }

            return workoutStepMesg;
        }

        private static WorkoutStepMesg CreateWorkoutStepSwimRest(int messageIndex, WktStepDuration durationType = WktStepDuration.Open, float? durationTime = null)
        {
            var workoutStepMesg = new WorkoutStepMesg();
            workoutStepMesg.SetMessageIndex((ushort)messageIndex);

            workoutStepMesg.SetDurationType(durationType);
            workoutStepMesg.SetDurationTime(durationTime);

            workoutStepMesg.SetTargetType(WktStepTarget.Open);

            workoutStepMesg.SetIntensity(Intensity.Rest);

            return workoutStepMesg;
        }
    }
}
