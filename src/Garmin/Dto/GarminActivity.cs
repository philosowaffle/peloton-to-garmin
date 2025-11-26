using System;

namespace Garmin.Dto
{
	public class GarminActivity
	{
		public long ActivityId { get; set; }
		public string ActivityName { get; set; }
		public string Description { get; set; }
		public DateTime? StartTimeLocal { get; set; }
		public DateTime? StartTimeGMT { get; set; }
		public string ActivityType { get; set; }
		public double Duration { get; set; }
		public double? ElapsedDuration { get; set; }
		public double? Distance { get; set; }
		public int? AverageHR { get; set; }
		public int? MaxHR { get; set; }
		public double? Calories { get; set; }
	}
}
