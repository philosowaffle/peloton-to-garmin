using System;
using System.Collections.Generic;
using System.Text;

namespace Peloton.Dto
{
	public class Workout
	{
		public long Created_At { get; set; }
		public string Device_Type { get; set; }
		public long End_Time { get; set; }
		public string Fitness_Discipline { get; set; }
		public bool Has_Pedaling_Metrics { get; set; }
		public bool Has_Leaderboard_Metrics { get; set; }
		public string Id { get; set; }
		public bool Is_ToTal_Work_Personal_Record { get; set; }
		public string Metrics_Type { get; set; }
		public string Name { get; set; }
		public string Platform { get; set; }
		public long Start_Time { get; set; }
		public string Status { get; set; }
	}
}
