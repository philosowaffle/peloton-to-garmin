using System.Collections.Generic;

namespace Common.Dto.Peloton
{
	public class RideDetails
	{
		public Ride Ride { get; set; }
		public TargetMetricsData Target_Metrics_Data { get; set; }
	}

	public class TargetMetricsData
	{
		public ICollection<TargetMetric> Target_Metrics { get; set; }
	}

	public class TargetMetric
	{
		public Offsets Offsets { get; set; }
		public string Segment_Type { get; set; }
		public ICollection<MetricData> Metrics { get; set; }
	}
}

public class Offsets
{
	public int Start { get; set; }
	public int End { get; set; }
}

public class MetricData
{
	public string Name { get; set; }
	public float Upper { get; set; }
	public float Lower { get; set; }
}