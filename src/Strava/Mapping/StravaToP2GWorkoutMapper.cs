using System;
using System.Collections.Generic;
using System.Linq;
using P2G.Common.Dto.Strava;
using P2G.Common.Models;
using P2G.Common.Enums;

namespace P2G.Strava.Mapping
{
    public interface IStravaToP2GWorkoutMapper
    {
        P2GWorkout Map(Activity activity, List<ActivityStream> streams);
        WorkoutType MapActivityType(string stravaType);
    }

    public class StravaToP2GWorkoutMapper : IStravaToP2GWorkoutMapper
    {
        public P2GWorkout Map(Activity activity, List<ActivityStream> streams)
        {
            var workout = new P2GWorkout
            {
                Id = activity.Id.ToString(),
                Name = activity.Name ?? $"Strava Activity {activity.Id}",
                StartTime = activity.StartDateLocal,
                EndTime = activity.StartDateLocal.AddSeconds(activity.ElapsedTime),
                DurationInSeconds = activity.ElapsedTime,
                WorkoutType = MapActivityType(activity.Type),
                Source = "Strava",
                Metrics = new WorkoutMetrics()
            };

            // Базовые метрики
            if (activity.Distance.HasValue)
                workout.Metrics.Distance = activity.Distance.Value / 1000.0; // метры -> км

            if (activity.TotalElevationGain.HasValue)
                workout.Metrics.ElevationGain = activity.TotalElevationGain.Value;

            if (activity.AverageSpeed.HasValue)
                workout.Metrics.AverageSpeed = activity.AverageSpeed.Value * 3.6; // м/с -> км/ч

            if (activity.MaxSpeed.HasValue)
                workout.Metrics.MaxSpeed = activity.MaxSpeed.Value * 3.6;

            if (activity.AverageWatts.HasValue)
                workout.Metrics.AveragePower = activity.AverageWatts.Value;

            if (activity.MaxWatts.HasValue)
                workout.Metrics.MaxPower = activity.MaxWatts.Value;

            if (activity.AverageHeartrate.HasValue)
                workout.Metrics.AverageHeartRate = activity.AverageHeartrate.Value;

            if (activity.MaxHeartrate.HasValue)
                workout.Metrics.MaxHeartRate = activity.MaxHeartrate.Value;

            if (activity.Calories.HasValue)
                workout.Metrics.Calories = activity.Calories.Value;

            // Обработка потоков данных
            ProcessStreams(workout, streams);

            // Обработка сегментов
            ProcessSegments(workout, activity.SplitsMetric);

            return workout;
        }

        private void ProcessStreams(P2GWorkout workout, List<ActivityStream> streams)
        {
            var timeStream = streams.FirstOrDefault(s => s.Type == "time");
            var distanceStream = streams.FirstOrDefault(s => s.Type == "distance");
            var latLngStream = streams.FirstOrDefault(s => s.Type == "latlng");
            var heartrateStream = streams.FirstOrDefault(s => s.Type == "heartrate");
            var powerStream = streams.FirstOrDefault(s => s.Type == "watts");
            var cadenceStream = streams.FirstOrDefault(s => s.Type == "cadence");
            var velocityStream = streams.FirstOrDefault(s => s.Type == "velocity_smooth");
            var altitudeStream = streams.FirstOrDefault(s => s.Type == "altitude");

            if (timeStream == null || timeStream.Data.Count == 0)
                return;

            var pointCount = timeStream.Data.Count;
            var points = new List<WorkoutPoint>(pointCount);

            for (int i = 0; i < pointCount; i++)
            {
                var point = new WorkoutPoint
                {
                    TimeOffset = timeStream.Data[i]
                };

                if (distanceStream != null && i < distanceStream.Data.Count)
                    point.Distance = distanceStream.Data[i];

                if (latLngStream != null && i < latLngStream.Data.Count)
                {
                    var coords = latLngStream.Data[i] as List<object>;
                    if (coords != null && coords.Count >= 2)
                    {
                        point.Latitude = Convert.ToDouble(coords[0]);
                        point.Longitude = Convert.ToDouble(coords[1]);
                    }
                }

                if (heartrateStream != null && i < heartrateStream.Data.Count)
                    point.HeartRate = heartrateStream.Data[i];

                if (powerStream != null && i < powerStream.Data.Count)
                    point.Power = powerStream.Data[i];

                if (cadenceStream != null && i < cadenceStream.Data.Count)
                    point.Cadence = cadenceStream.Data[i];

                if (velocityStream != null && i < velocityStream.Data.Count)
                    point.Speed = velocityStream.Data[i] * 3.6; // м/с -> км/ч

                if (altitudeStream != null && i < altitudeStream.Data.Count)
                    point.Altitude = altitudeStream.Data[i];

                points.Add(point);
            }

            workout.Points = points;
        }

        private void ProcessSegments(P2GWorkout workout, List<Split> splits)
        {
            if (splits == null || splits.Count == 0)
                return;

            workout.Splits = new List<WorkoutSplit>();

            foreach (var split in splits)
            {
                var workoutSplit = new WorkoutSplit
                {
                    SplitNumber = split.SplitNumber,
                    Distance = split.Distance / 1000.0, // метры -> км
                    DurationInSeconds = split.ElapsedTime,
                    AverageSpeed = split.AverageSpeed * 3.6, // м/с -> км/ч
                    AverageHeartRate = split.AverageHeartrate,
                    Pace = split.Pace
                };

                workout.Splits.Add(workoutSplit);
            }
        }

        public WorkoutType MapActivityType(string stravaType)
        {
            return stravaType.ToLower() switch
            {
                "run" => WorkoutType.OutdoorRunning,
                "trail_run" => WorkoutType.OutdoorRunning,
                "virtual_run" => WorkoutType.TreadmillRunning,
                "treadmill_running" => WorkoutType.TreadmillRunning,
                
                "ride" => WorkoutType.Cycling,
                "virtual_ride" => WorkoutType.IndoorCycling,
                "gravel_ride" => WorkoutType.Cycling,
                "mountain_bike_ride" => WorkoutType.Cycling,
                "e_bike_ride" => WorkoutType.Cycling,
                
                "walk" => WorkoutType.Walking,
                "hike" => WorkoutType.Hiking,
                
                "weight_training" => WorkoutType.StrengthTraining,
                "workout" => WorkoutType.StrengthTraining,
                "crossfit" => WorkoutType.StrengthTraining,
                
                "swim" => WorkoutType.PoolSwimming,
                "open_water_swim" => WorkoutType.OpenWaterSwimming,
                
                "yoga" => WorkoutType.Yoga,
                "pilates" => WorkoutType.Pilates,
                
                "ski" => WorkoutType.Skiing,
                "snowboard" => WorkoutType.Snowboarding,
                "ice_skate" => WorkoutType.IceSkating,
                
                "kayaking" => WorkoutType.Kayaking,
                "stand_up_paddling" => WorkoutType.StandUpPaddling,
                "rowing" => WorkoutType.Rowing,
                
                _ => WorkoutType.Other
            };
        }
    }
}
