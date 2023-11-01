using System.Collections.Generic;

namespace Common.Dto.Peloton;

public class Segment
{
	public int Length { get; set; }
	public int Start_Time_Offset { get; set; }
	public ICollection<SubSegment> SubSegments_V2 { get; set; }
}

public record SubSegment
{
	public string Type { get; init; } // enum
	public int? Offset { get; init; }
	public int? Length { get; init; }
	public int? Rounds { get; init; }
	public ICollection<Movement> Movements { get; init; }
}

public record Movement
{
	public string Id { get; init; }
	public string Name { get; init; }
	public string Weight_Level { get; init; } // enum { heavy, medium, ??? }
}
