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
