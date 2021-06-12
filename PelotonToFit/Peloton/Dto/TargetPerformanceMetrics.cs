﻿using System.Collections.Generic;

namespace Peloton.Dto
{
	public class TargetPerformanceMetrics
	{
		public ICollection<TargetGraphMetrics> Target_Graph_Metrics { get; set; }
		public int Cadence_Time_In_Range { get; set; }
		public int Resistance_Time_In_Range { get; set; }
	}

	public class TargetGraphMetrics
	{
		public GraphData Graph_Data { get; set; }
		public int Max { get; set; }
		public int Min { get; set; }
		public int Average { get; set; }
		public string Type { get; set; } // enum
	}

	public class GraphData
	{
		public ICollection<int> Upper { get; set; }
		public ICollection<int> Lower { get; set; }
		public ICollection<int> Average { get; set; }
	}
}
