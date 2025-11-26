using System;
using System.Collections.Generic;
using System.Linq;
using Common.Dto;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class PelotonParser
    {
        public class Sample
        {
            public DateTime Time;
            public int? HeartRate;
            public double? Power;
            public int? Cadence;
        }

        public static List<Sample> ParsePelotonToSeries(P2GWorkout workout)
        {
            var outSamples = new List<Sample>();
            if (workout == null || workout.WorkoutSamples == null)
                return outSamples;

            try
            {
                var metrics = workout.WorkoutSamples.Metrics;
                if (metrics == null || metrics.Count == 0)
                    return outSamples;

                var startTime = workout.Workout.Created ?? DateTime.UtcNow;
                var sampleCount = metrics.FirstOrDefault()?.Values?.Count ?? 0;

                for (int i = 0; i < sampleCount; i++)
                {
                    var sample = new Sample
                    {
                        Time = startTime.AddSeconds(i)
                    };

                    foreach (var metric in metrics)
                    {
                        if (metric.Values == null || i >= metric.Values.Count)
                            continue;

                        var value = metric.Values[i];
                        if (value == null)
                            continue;

                        switch (metric.Slug)
                        {
                            case "heart_rate":
                                if (int.TryParse(value.Value?.ToString(), out var hr))
                                    sample.HeartRate = hr;
                                break;
                            case "output":
                                if (double.TryParse(value.Value?.ToString(), out var power))
                                    sample.Power = power;
                                break;
                            case "cadence":
                                if (int.TryParse(value.Value?.ToString(), out var cadence))
                                    sample.Cadence = cadence;
                                break;
                        }
                    }

                    outSamples.Add(sample);
                }
            }
            catch
            {
                // Return empty if parsing fails
            }

            return outSamples;
        }
    }
}
