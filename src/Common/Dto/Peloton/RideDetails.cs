using System.Collections.Generic;

namespace Common.Dto.Peloton;

public record RideDetails
{
	public Ride Ride { get; init; }
	public TargetMetricsData Target_Metrics_Data { get; init; }
	public Segments Segments { get; init; }
}

public record Segments
{
	public ICollection<Segment> Segment_List { get; init; }
}

public record TargetMetricsData
{
	public ICollection<TargetMetric> Target_Metrics { get; init; }
}

public record TargetMetric
{
	public Offsets Offsets { get; init; }
	public string Segment_Type { get; init; }
	public ICollection<MetricData> Metrics { get; init; }
}

public record Offsets
{
	public int Start { get; init; }
	public int End { get; init; }
}

public record MetricData
{
	public string Name { get; init; }
	public float Upper { get; init; }
	public float Lower { get; init; }
}