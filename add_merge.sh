#!/usr/bin/env bash
set -e
echo "Creating Merge feature files..."

# helper to create directories and files
write_file() {
  local file="$1"
  shift
  local content="$*"
  mkdir -p "$(dirname "$file")"
  cat > "$file" <<'EOF'
'"${content//"'/"'"'}"
EOF
}

# Create files under src/Sync/Merge
mkdir -p src/Sync/Merge/Utilities

cat > src/Sync/Merge/MergeOptions.cs <<'EOF'
using System;

namespace PelotonToGarmin.Sync.Merge
{
    /// <summary>
    /// Tweakable options for the merge engine.
    /// </summary>
    public class MergeOptions
    {
        public int MatchTimeWindowSeconds { get; set; } = 300;
        public double MatchDurationDiffPct { get; set; } = 0.15;
        public double MatchScoreThreshold { get; set; } = 0.50;
        public bool AutoApproveEnabled { get; set; } = true;
        public double AutoApproveScoreThreshold { get; set; } = 0.75;
        public int InterpolationResolutionSeconds { get; set; } = 1;
    }
}
EOF

cat > src/Sync/Merge/MergeScoreCalculator.cs <<'EOF'
using System;

namespace PelotonToGarmin.Sync.Merge
{
    public static class MergeScoreCalculator
    {
        public static double ScoreCandidate(DateTime? pelotonStart, double pelotonDuration, DateTime? garminStart, double garminDuration, MergeOptions opts)
        {
            double timeScore = 0.0;
            double durationScore = 0.0;
            if (pelotonStart.HasValue && garminStart.HasValue)
            {
                var dt = Math.Abs((pelotonStart.Value - garminStart.Value).TotalSeconds);
                timeScore = Math.Max(0.0, 1.0 - (dt / opts.MatchTimeWindowSeconds));
            }
            if (pelotonDuration > 0 && garminDuration > 0)
            {
                var diff = Math.Abs(pelotonDuration - garminDuration) / pelotonDuration;
                durationScore = Math.Max(0.0, 1.0 - (diff / opts.MatchDurationDiffPct));
            }
            return 0.6 * timeScore + 0.4 * durationScore;
        }
    }
}
EOF

cat > src/Sync/Merge/MergeResult.cs <<'EOF'
using System;

namespace PelotonToGarmin.Sync.Merge
{
    public class MergeResult
    {
        public string PelotonId { get; set; }
        public string GarminActivityId { get; set; }
        public double Score { get; set; }
        public string MergedTcxPath { get; set; }
        public string MergedFitPath { get; set; }
        public bool AutoApproved { get; set; }
        public string Note { get; set; }
    }
}
EOF

cat > src/Sync/Merge/MergeEngine.cs <<'EOF'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PelotonToGarmin.Sync.Merge.Utilities;

namespace PelotonToGarmin.Sync.Merge
{
    public class MergeEngine
    {
        private readonly MergeOptions _opts;
        private readonly dynamic _pelotonClient;
        private readonly dynamic _garminClient;
        private readonly string _dataDir;

        public MergeEngine(MergeOptions opts, dynamic pelotonClient, dynamic garminClient, string dataDirectory = "data/merged")
        {
            _opts = opts ?? new MergeOptions();
            _pelotonClient = pelotonClient ?? throw new ArgumentNullException(nameof(pelotonClient));
            _garminClient = garminClient ?? throw new ArgumentNullException(nameof(garminClient));
            _dataDir = dataDirectory;
            Directory.CreateDirectory(_dataDir);
        }

        public MergeResult PreviewMerge(string pelotonId)
        {
            var pDetails = _pelotonClient.DownloadWorkout(pelotonId);
            if (pDetails == null) throw new InvalidOperationException("Peloton workout not found");

            DateTime? pStart = Utilities.Helpers.ExtractStartUtc(pDetails);
            double pDuration = Utilities.Helpers.ExtractDurationSeconds(pDetails);

            var gCandidates = _garminClient.ListRecentActivities(100);
            double bestScore = 0.0;
            dynamic best = null;
            foreach (var c in gCandidates)
            {
                DateTime? gStart = Utilities.Helpers.ExtractGarminStartUtc(c);
                double gDur = Utilities.Helpers.ExtractGarminDurationSeconds(c);
                var score = MergeScoreCalculator.ScoreCandidate(pStart, pDuration, gStart, gDur, _opts);
                if (score > bestScore) { bestScore = score; best = c; }
            }

            string garTcx = null;
            if (best != null && bestScore >= _opts.MatchScoreThreshold)
            {
                garTcx = _garminClient.DownloadActivityTcx(best.ActivityId);
            }

            var gSeries = !string.IsNullOrEmpty(garTcx) ? TcxParser.ParseTcxToSeries(garTcx) : null;
            var pSeries = PelotonParser.ParsePelotonToSeries(pDetails);

            var merged = MergeSeries.Merge(gSeries, pSeries, _opts.InterpolationResolutionSeconds);

            if (merged == null || merged.Count == 0)
            {
                return new MergeResult { PelotonId = pelotonId, GarminActivityId = best?.ActivityId, Score = bestScore, Note = "no merged samples" };
            }

            var tsLabel = DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
            var tcxPath = Path.Combine(_dataDir, $"merged-{pelotonId}-{tsLabel}.tcx");
            TcxWriter.WriteMergedTcx(merged, merged.First().Time, tcxPath);
            var fitPath = Path.Combine(_dataDir, $"merged-{pelotonId}-{tsLabel}.fit");
            FitWriter.WriteMinimalFit(merged, fitPath);

            return new MergeResult
            {
                PelotonId = pelotonId,
                GarminActivityId = best?.ActivityId,
                Score = bestScore,
                MergedTcxPath = tcxPath,
                MergedFitPath = fitPath,
                AutoApproved = _opts.AutoApproveEnabled && bestScore >= _opts.AutoApproveScoreThreshold,
                Note = best == null ? "no candidate" : "preview generated"
            };
        }

        public MergeResult ApproveAndUpload(MergeResult preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            string uploadedId = null;
            if (File.Exists(preview.MergedFitPath))
            {
                uploadedId = _garminClient.UploadActivity(preview.MergedFitPath);
            }
            if (uploadedId == null && File.Exists(preview.MergedTcxPath))
            {
                uploadedId = _garminClient.UploadActivity(preview.MergedTcxPath);
            }
            preview.GarminActivityId = uploadedId;
            preview.AutoApproved = true;
            preview.Note = uploadedId != null ? "uploaded" : "upload_failed";
            return preview;
        }
    }
}
EOF

# Utilities
cat > src/Sync/Merge/Utilities/TcxParser.cs <<'EOF'
using System;
using System.Collections.Generic;
using System.Xml;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class TcxParser
    {
        public class Sample { public DateTime Time; public int? HeartRate; public double? Power; public int? Cadence; public double? Lat; public double? Lon; }

        public static List<Sample> ParseTcxToSeries(string tcxXml)
        {
            var outSamples = new List<Sample>();
            if (string.IsNullOrEmpty(tcxXml)) return outSamples;
            var doc = new XmlDocument();
            doc.LoadXml(tcxXml);
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("t", "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2");
            var nodes = doc.SelectNodes("//t:Trackpoint", nsManager);
            if (nodes == null) return outSamples;
            foreach (XmlNode node in nodes)
            {
                try
                {
                    var timeNode = node.SelectSingleNode("t:Time", nsManager);
                    var hrNode = node.SelectSingleNode(".//t:HeartRateBpm/t:Value", nsManager);
                    var latNode = node.SelectSingleNode("t:Position/t:LatitudeDegrees", nsManager);
                    var lonNode = node.SelectSingleNode("t:Position/t:LongitudeDegrees", nsManager);
                    var cadNode = node.SelectSingleNode("t:Cadence", nsManager);
                    var sample = new Sample();
                    if (timeNode != null) sample.Time = DateTime.Parse(timeNode.InnerText, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                    if (hrNode != null) sample.HeartRate = int.Parse(hrNode.InnerText);
                    if (cadNode != null) sample.Cadence = int.Parse(cadNode.InnerText);
                    if (latNode != null) sample.Lat = double.Parse(latNode.InnerText);
                    if (lonNode != null) sample.Lon = double.Parse(lonNode.InnerText);
                    outSamples.Add(sample);
                }
                catch { }
            }
            return outSamples;
        }
    }
}
EOF

cat > src/Sync/Merge/Utilities/PelotonParser.cs <<'EOF'
using System;
using System.Collections.Generic;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class PelotonParser
    {
        public class Sample { public DateTime Time; public int? HeartRate; public double? Power; public int? Cadence; }

        public static List<Sample> ParsePelotonToSeries(dynamic pDetails)
        {
            var outSamples = new List<Sample>();
            if (pDetails == null) return outSamples;
            try
            {
                var series = pDetails.series as IEnumerable<dynamic>;
                if (series != null)
                {
                    foreach (var s in series)
                    {
                        try
                        {
                            DateTime t = DateTime.Parse((string)s.timestamp);
                            var samp = new Sample { Time = t };
                            int hr; double pw;
                            if (int.TryParse((string)(s.heart_rate ?? ""), out hr)) samp.HeartRate = hr;
                            if (double.TryParse((string)(s.power ?? ""), out pw)) samp.Power = pw;
                            outSamples.Add(samp);
                        } catch { }
                    }
                }
                else
                {
                    var start = DateTime.Parse((string)(pDetails.start_time ?? pDetails.created_at));
                    outSamples.Add(new Sample { Time = start, HeartRate = (int?)(pDetails.average_heart_rate ?? null), Power = (double?)(pDetails.average_watts ?? null) });
                }
            }
            catch { }
            return outSamples;
        }
    }
}
EOF

cat > src/Sync/Merge/Utilities/MergeSeries.cs <<'EOF'
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
EOF

cat > src/Sync/Merge/Utilities/TcxWriter.cs <<'EOF'
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
EOF

cat > src/Sync/Merge/Utilities/FitWriter.cs <<'EOF'
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
EOF

cat > src/Sync/Merge/Utilities/Helpers.cs <<'EOF'
using System;

namespace PelotonToGarmin.Sync.Merge.Utilities
{
    public static class Helpers
    {
        public static DateTime? ExtractStartUtc(dynamic pDetails)
        {
            try
            {
                if (pDetails == null) return null;
                if (pDetails.start_time != null) return DateTime.Parse((string)pDetails.start_time).ToUniversalTime();
                if (pDetails.created_at != null) return DateTime.Parse((string)pDetails.created_at).ToUniversalTime();
            }
            catch { }
            return null;
        }

        public static double ExtractDurationSeconds(dynamic pDetails)
        {
            try
            {
                if (pDetails == null) return 0;
                if (pDetails.duration != null) return Convert.ToDouble(pDetails.duration);
                if (pDetails.duration_seconds != null) return Convert.ToDouble(pDetails.duration_seconds);
                if (pDetails.elapsed_seconds != null) return Convert.ToDouble(pDetails.elapsed_seconds);
            }
            catch { }
            return 0;
        }

        public static DateTime? ExtractGarminStartUtc(dynamic g)
        {
            try
            {
                if (g == null) return null;
                if (g.startTimeLocal != null) return DateTime.Parse((string)g.startTimeLocal).ToUniversalTime();
                if (g.startTimeGMT != null) return DateTime.Parse((string)g.startTimeGMT).ToUniversalTime();
            }
            catch { }
            return null;
        }

        public static double ExtractGarminDurationSeconds(dynamic g)
        {
            try
            {
                if (g == null) return 0;
                if (g.duration != null) return Convert.ToDouble(g.duration);
                if (g.elapsedDuration != null) return Convert.ToDouble(g.elapsedDuration);
            }
            catch { }
            return 0;
        }
    }
}
EOF

# Api.Service controller
mkdir -p src/Api.Service/Controllers
cat > src/Api.Service/Controllers/MergeController.cs <<'EOF'
using System;
using Microsoft.AspNetCore.Mvc;
using PelotonToGarmin.Sync.Merge;

namespace PelotonToGarmin.Api.Service.Controllers
{
    [ApiController]
    [Route("api/merge")]
    public class MergeController : ControllerBase
    {
        private readonly MergeEngine _engine;

        public MergeController(MergeEngine engine)
        {
            _engine = engine;
        }

        [HttpPost("preview/{pelotonId}")]
        public IActionResult Preview(string pelotonId)
        {
            try
            {
                var result = _engine.PreviewMerge(pelotonId);
                return Ok(new { success = true, result.PelotonId, result.GarminActivityId, result.Score, result.MergedTcxPath, result.MergedFitPath, result.AutoApproved });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("approve")]
        public IActionResult Approve([FromBody] MergeResult preview)
        {
            try
            {
                var res = _engine.ApproveAndUpload(preview);
                return Ok(new { success = true, uploaded = res.GarminActivityId, note = res.Note });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
EOF

# ClientUI Blazor WASM page
mkdir -p src/ClientUI/Pages
cat > src/ClientUI/Pages/Merge.razor <<'EOF'
@page "/merge"
@inject HttpClient Http

<h1>Merge Dashboard</h1>
<div>
  <input @bind="pelotonId" placeholder="Peloton workout id" />
  <button @onclick="Preview">Preview</button>
</div>
@if (preview != null)
{
  <div class="card">
    <h3>Preview for @preview.PelotonId</h3>
    <p>Candidate Garmin Id: @preview.GarminActivityId</p>
    <p>Score: @preview.Score</p>
    <p>AutoApproved: @preview.AutoApproved</p>
    <button @onclick="Approve">Approve & Upload</button>
  </div>
}

@code {
  private string pelotonId;
  private dynamic preview;

  private async Task Preview()
  {
    preview = null;
    var resp = await Http.PostAsync($"/api/merge/preview/{pelotonId}", null);
    if (resp.IsSuccessStatusCode)
    {
      preview = await resp.Content.ReadFromJsonAsync<dynamic>();
      if (preview != null && preview.result != null) preview = preview.result;
    }
    else
    {
      var txt = await resp.Content.ReadAsStringAsync();
      preview = new { PelotonId = pelotonId, Error = txt };
    }
  }

  private async Task Approve()
  {
    if (preview == null) return;
    var payload = System.Text.Json.JsonSerializer.Serialize(preview);
    var resp = await Http.PostAsync("/api/merge/approve", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
  }
}
EOF

echo "Files created. Please add the new sources to the appropriate .csproj(s), register MergeEngine and MergeOptions in DI, then build."
echo "Recommended DI (example in Program.cs):"
cat <<'EOF'
/* Example DI snippet to add to Program.cs of the API project (Api.Service):
   using PelotonToGarmin.Sync.Merge;
   // elsewhere in builder.Services
   var mergeOpts = new MergeOptions(); // or bind from config
   builder.Services.AddSingleton(mergeOpts);
   // register your existing Peloton and Garmin client implementations (adapt names/methods)
   builder.Services.AddSingleton<IPelotonClient, PelotonClient>(); // adjust to your repo's types
   builder.Services.AddSingleton<IGarminClient, GarminClient>();
   builder.Services.AddSingleton<MergeEngine>();
*/
EOF

echo "Done."
