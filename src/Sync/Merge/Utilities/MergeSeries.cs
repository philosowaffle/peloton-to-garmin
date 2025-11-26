using System;
using System.Collections.Generic;
using System.Linq;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class MergeSeries
    {
        public class UnifiedSample
        {
            public DateTime Time;
            public int? HeartRate;
            public double? Power;
            public int? Cadence;
            public double? Lat;
            public double? Lon;
            public string HrSource;
        }

        public static List<UnifiedSample> Merge(List<TcxParser.Sample> gar, List<PelotonParser.Sample> pel, int resolutionSeconds = 1)
        {
            var samples = new List<UnifiedSample>();
            if ((gar == null || gar.Count == 0) && (pel == null || pel.Count == 0)) return samples;
            var times = new List<DateTime>();
            if (gar != null && gar.Count > 0) times.AddRange(gar.Select(g => g.Time));
            if (pel != null && pel.Count > 0) times.AddRange(pel.Select(p => p.Time));
            var start = times.Min();
            var end = times.Max();
            for (var t = start; t <= end; t = t.AddSeconds(resolutionSeconds))
            {
                var us = new UnifiedSample { Time = t };
                var gAt = gar?.FirstOrDefault(x => x.Time == t);
                if (gAt != null)
                {
                    us.HeartRate = gAt.HeartRate;
                    us.Cadence = gAt.Cadence;
                    us.Lat = gAt.Lat;
                    us.Lon = gAt.Lon;
                    us.HrSource = gAt.HeartRate.HasValue ? "garmin" : null;
                }
                var pAt = pel?.FirstOrDefault(x => x.Time == t);
                if (pAt != null)
                {
                    if (!us.HeartRate.HasValue && pAt.HeartRate.HasValue) { us.HeartRate = pAt.HeartRate; us.HrSource = "peloton"; }
                    if (!us.Power.HasValue && pAt.Power.HasValue) us.Power = pAt.Power;
                    if (!us.Cadence.HasValue && pAt.Cadence.HasValue) us.Cadence = pAt.Cadence;
                }
                samples.Add(us);
            }
            return samples;
        }
    }
}
