using System;

namespace Common.Dto.Peloton
{
	public class Segment
	{
		public string Id { get; set; }
		public int Length { get; set; }
		public int Start_Time_Offset { get; set; }
		public Uri Icon_Url { get; set; }
		public double Intensity_In_Mets { get; set; }
		public string Metrics_Type { get; set; } // enum
		public string Icon_Name { get; set; }
		public string Icon_Slug { get; set; }
		public string Name { get; set; }
	}
}
