using System;
using Common.Dto;

namespace PelotonToGarmin.Sync.Merge
{
    public class MergeOptions
    {
        public int MatchTimeWindowSeconds { get; set; } = 300;
        public double MatchDurationDiffPct { get; set; } = 0.15;
        public double MatchScoreThreshold { get; set; } = 0.50;
        public bool AutoApproveEnabled { get; set; } = true;
        public double AutoApproveScoreThreshold { get; set; } = 0.75;
        public int InterpolationResolutionSeconds { get; set; } = 1;

        public static MergeOptions FromSettings(MergeSettings settings)
        {
            return new MergeOptions
            {
                MatchTimeWindowSeconds = settings.MatchTimeWindowSeconds,
                MatchDurationDiffPct = settings.MatchDurationDiffPct,
                MatchScoreThreshold = settings.MatchScoreThreshold,
                AutoApproveEnabled = settings.AutoApproveEnabled,
                AutoApproveScoreThreshold = settings.AutoApproveScoreThreshold,
                InterpolationResolutionSeconds = settings.InterpolationResolutionSeconds
            };
        }
    }
}
