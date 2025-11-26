using System;
using System.Collections.Generic;
using System.IO;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class FitWriter
    {
        public static string WriteMinimalFit(List<MergeSeries.UnifiedSample> samples, string destPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? ".");
                using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(System.Text.Encoding.ASCII.GetBytes("FITMIN"));
                    foreach (var s in samples)
                    {
                        var ticks = s.Time.ToUniversalTime().Ticks;
                        bw.Write(ticks);
                        bw.Write(s.HeartRate ?? 0);
                        bw.Write((int)(s.Power ?? 0));
                    }
                }
                return destPath;
            }
            catch
            {
                return null;
            }
        }
    }
}
