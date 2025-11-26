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
