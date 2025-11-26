using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class TcxWriter
    {
        public static void WriteMergedTcx(List<MergeSeries.UnifiedSample> samples, DateTime start, string destPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ".");
            using (var sw = new StreamWriter(destPath, false, Encoding.UTF8))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<TrainingCenterDatabase xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\">");
                sw.WriteLine("  <Activities>");
                sw.WriteLine("    <Activity Sport=\"Biking\">");
                sw.WriteLine($"      <Id>{start.ToString(\"o\")}</Id>");
                sw.WriteLine($"      <Lap StartTime=\"{start.ToString(\"o\")}\">");
                sw.WriteLine($"        <TotalTimeSeconds>{(samples.Count>1 ? (samples.Last().Time - samples.First().Time).TotalSeconds : 0):F1}</TotalTimeSeconds>");
                sw.WriteLine("        <Track>");
                foreach (var s in samples)
                {
                    sw.WriteLine("          <Trackpoint>");
                    sw.WriteLine($"            <Time>{s.Time.ToString(\"o\")}</Time>");
                    if (s.Lat.HasValue && s.Lon.HasValue) sw.WriteLine($"            <Position><LatitudeDegrees>{s.Lat.Value}</LatitudeDegrees><LongitudeDegrees>{s.Lon.Value}</LongitudeDegrees></Position>");
                    if (s.HeartRate.HasValue) sw.WriteLine($"            <HeartRateBpm><Value>{s.HeartRate.Value}</Value></HeartRateBpm>");
                    if (s.Power.HasValue) sw.WriteLine($"            <Extensions><TPX xmlns=\"http://www.garmin.com/xmlschemas/ActivityExtension/v2\"><Watts>{(int)s.Power.Value}</Watts></TPX></Extensions>");
                    if (s.Cadence.HasValue) sw.WriteLine($"            <Cadence>{s.Cadence.Value}</Cadence>");
                    sw.WriteLine("          </Trackpoint>");
                }
                sw.WriteLine("        </Track>");
                sw.WriteLine("      </Lap>");
                sw.WriteLine("    </Activity>");
                sw.WriteLine("  </Activities>");
                sw.WriteLine("</TrainingCenterDatabase>");
            }
        }
    }
}
