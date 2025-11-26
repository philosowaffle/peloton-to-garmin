using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PelotonToGarmin.Sync.Merge.Utilities;
using Peloton;
using Garmin;
using Garmin.Auth;
using Garmin.Dto;
using Common.Dto.Peloton;
using Serilog;
using Common.Observe;

namespace PelotonToGarmin.Sync.Merge
{
    public class MergeEngine
    {
        private static readonly ILogger _logger = LogContext.ForClass<MergeEngine>();
        private readonly MergeOptions _opts;
        private readonly IPelotonService _pelotonService;
        private readonly IGarminApiClient _garminClient;
        private readonly IGarminAuthenticationService _authService;
        private readonly string _dataDir;

        public MergeEngine(MergeOptions opts, IPelotonService pelotonService, IGarminApiClient garminClient, IGarminAuthenticationService authService, string dataDirectory = "data/merged")
        {
            _opts = opts ?? new MergeOptions();
            _pelotonService = pelotonService ?? throw new ArgumentNullException(nameof(pelotonService));
            _garminClient = garminClient ?? throw new ArgumentNullException(nameof(garminClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _dataDir = dataDirectory;
            Directory.CreateDirectory(_dataDir);
        }

        public async Task<MergeResult> PreviewMergeAsync(string pelotonId)
        {
            try
            {
                _logger.Information("Starting merge preview for Peloton workout {pelotonId}", pelotonId);

                var workout = new Workout() { Id = pelotonId };
                var workouts = await _pelotonService.GetWorkoutDetailsAsync(new[] { workout });
                var pDetails = workouts?.FirstOrDefault();
                
                if (pDetails == null)
                    throw new InvalidOperationException("Peloton workout not found");

                DateTime? pStart = pDetails.Workout.Created;
                double pDuration = pDetails.Workout.Ride?.Duration ?? 0;

                var auth = await _authService.GetGarminAuthenticationAsync();
                var gCandidates = await _garminClient.GetRecentActivitiesAsync(100, auth);
                
                double bestScore = 0.0;
                GarminActivity best = null;
                
                foreach (var c in gCandidates)
                {
                    DateTime? gStart = c.StartTimeGMT ?? c.StartTimeLocal;
                    double gDur = c.Duration;
                    var score = MergeScoreCalculator.ScoreCandidate(pStart, pDuration, gStart, gDur, _opts);
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }

                string garTcx = null;
                if (best != null && bestScore >= _opts.MatchScoreThreshold)
                {
                    garTcx = await _garminClient.GetActivityTcxAsync(best.ActivityId, auth);
                }

                var gSeries = !string.IsNullOrEmpty(garTcx) ? TcxParser.ParseTcxToSeries(garTcx) : null;
                var pSeries = PelotonParser.ParsePelotonToSeries(pDetails);

                var merged = MergeSeries.Merge(gSeries, pSeries, _opts.InterpolationResolutionSeconds);

                if (merged == null || merged.Count == 0)
                {
                    _logger.Warning("No merged samples generated for Peloton workout {pelotonId}", pelotonId);
                    return new MergeResult
                    {
                        PelotonId = pelotonId,
                        GarminActivityId = best?.ActivityId.ToString(),
                        Score = bestScore,
                        Note = "no merged samples"
                    };
                }

                var tsLabel = DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
                var tcxPath = Path.Combine(_dataDir, $"merged-{pelotonId}-{tsLabel}.tcx");
                TcxWriter.WriteMergedTcx(merged, merged.First().Time, tcxPath);
                var fitPath = Path.Combine(_dataDir, $"merged-{pelotonId}-{tsLabel}.fit");
                FitWriter.WriteMinimalFit(merged, fitPath);

                _logger.Information("Merge preview complete for {pelotonId}. Score: {score}, Best match: {garminId}", 
                    pelotonId, bestScore, best?.ActivityId);

                return new MergeResult
                {
                    PelotonId = pelotonId,
                    GarminActivityId = best?.ActivityId.ToString(),
                    Score = bestScore,
                    MergedTcxPath = tcxPath,
                    MergedFitPath = fitPath,
                    AutoApproved = _opts.AutoApproveEnabled && bestScore >= _opts.AutoApproveScoreThreshold,
                    Note = best == null ? "no candidate" : "preview generated"
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to preview merge for Peloton workout {pelotonId}", pelotonId);
                throw;
            }
        }

        public async Task<MergeResult> ApproveAndUploadAsync(MergeResult preview)
        {
            if (preview == null)
                throw new ArgumentNullException(nameof(preview));

            try
            {
                _logger.Information("Approving and uploading merged workout {pelotonId}", preview.PelotonId);

                var auth = await _authService.GetGarminAuthenticationAsync();
                string uploadedFile = null;

                if (File.Exists(preview.MergedFitPath))
                {
                    await _garminClient.UploadActivity(preview.MergedFitPath, ".fit", auth);
                    uploadedFile = preview.MergedFitPath;
                }
                else if (File.Exists(preview.MergedTcxPath))
                {
                    await _garminClient.UploadActivity(preview.MergedTcxPath, ".tcx", auth);
                    uploadedFile = preview.MergedTcxPath;
                }

                preview.AutoApproved = true;
                preview.Note = uploadedFile != null ? "uploaded" : "upload_failed";

                _logger.Information("Merge upload complete for {pelotonId}. Status: {note}", preview.PelotonId, preview.Note);

                return preview;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to approve and upload merged workout {pelotonId}", preview.PelotonId);
                throw;
            }
        }
    }
}
