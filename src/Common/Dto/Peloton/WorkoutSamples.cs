using System.Collections.Generic;

namespace Common.Dto.Peloton
{
	public class WorkoutSamples
	{
		public int Duration { get; set; }
		public bool Is_Class_Plan_Shown { get; set; }
		public ICollection<Segment> Segment_List { get; set; }
		public ICollection<int> Seconds_Since_Pedaling_Start { get; set; }
		public ICollection<AverageSummary> Average_Summaries { get; set; }
		public ICollection<Summary> Summaries { get; set; }
		public ICollection<Metric> Metrics { get; set; }
		public bool Has_Apple_Watch_Metrcis { get; set; }
		public ICollection<LocationData> Location_Data { get; set; }
		public bool? Is_Location_Data_Accurate { get; set; }
		// splits data
		public TargetPerformanceMetrics Target_Performance_Metrics { get; set; }
		// effort zones
	}

	public class LocationData
	{
		public string Segment_Id { get; set; }
		public bool Is_Gap { get; set; }
		public ICollection<Coordinate> Coordinates { get; set; }
	}

	public class Coordinate
	{
		public float? Accuracy { get; set; }
		public float Distance { get; set; }
		public string Distance_Display_Unit { get; set; }
		public float Latitude { get; set; }
		public float Longitude { get; set; }
		public int Seconds_Offset_From_Start { get; set; }
	}
}
