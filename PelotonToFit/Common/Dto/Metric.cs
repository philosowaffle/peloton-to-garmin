using System.Collections.Generic;

namespace Common.Dto
{
	public class Metric
	{
		public string Display_Name { get; set; }
		public string Display_Unit { get; set; }
		public double Max_Value { get; set; }
		public double Average_Value { get; set; }
		public double[] Values { get; set; }
		public string Slug { get; set; } // enum
		public ICollection<Zone> Zones { get; set; }
		public int Missing_Data_Duration { get; set; }
	}
}
