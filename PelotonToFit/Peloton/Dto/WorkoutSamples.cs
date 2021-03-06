using System.Collections.Generic;

namespace Peloton.Dto
{
	public class WorkoutSamples
	{
		public int Duration { get; set; }
		public bool Is_Class_Plan_Shown { get; set; }
		public ICollection<Segment> Segment_List { get; set; }
		public ICollection<int> Seconds_Since_Pedaling_Start { get; set; }
		public ICollection<AverageSummary> Average_Summaries {get; set; }
		public ICollection<Summary> Summaries { get; set; }
		public ICollection<Metric> Metrics { get; set; }
		public bool Has_Apple_Watch_Metrcis { get; set; }
		// location data
		public bool? Is_Location_Data_Accurate { get; set; }
		// splits data
		public TargetPerformanceMetrics Target_Performance_Metrics { get; set; }
		// effort zones
	}
}
