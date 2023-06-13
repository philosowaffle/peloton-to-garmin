using System.Collections.Generic;

namespace Common.Dto.Peloton;

public record RideSegments
{
	public Segments Segments { get; init; }
}

public record Segments
{
	public ICollection<Segment> Segment_List { get; init; }
}
