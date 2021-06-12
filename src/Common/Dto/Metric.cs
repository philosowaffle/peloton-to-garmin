using Serilog;
using System;
using System.Collections.Generic;

namespace Common.Dto
{
	public class Metric
	{
		public string Display_Name { get; set; }
		public string Display_Unit { get; set; }
		public double Max_Value { get; set; }
		public double Average_Value { get; set; }
		public double?[] Values { get; set; }
		public string Slug { get; set; } // enum
		public ICollection<Zone> Zones { get; set; }
		public int Missing_Data_Duration { get; set; }
		public ICollection<Metric> Alternatives { get; set; }
	}

	public static class MetricExtensions 
	{
		public static double GetValue(this Metric metric, int index)
		{
			try
			{
				return metric.Values[index].GetValueOrDefault(0);
			} 
			catch(IndexOutOfRangeException)
			{
				Log.Debug("Index out of range exception. Returning 0 for Metric value. Index: {@Index}, Metric: {@Slug}", index, metric.Slug);
				return 0;
			}
		}
	}

}
