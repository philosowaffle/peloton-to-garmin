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
		public string Peloton_Id { get; set; }
		public string Platform { get; set; }
		public long Start_Time { get; set; }
		public string Strava_Id { get; set; }
		public string Status { get; set; }
		public string Timezone { get; set; }
		public string Title { get; set; }
		public double Total_Work { get; set; }
		public string User_Id { get; set; }
		public int Total_Video_Watch_Time_Seconds { get; set; }
		public int Total_Video_Buffering_Seconds { get; set; }
		public int V2_Total_Video_Watch_Time_Seconds { get; set; }
		public int V2_Total_Video_Buffering_Seconds { get; set; }
		// User object
		public long Created { get; set; }
		public long Device_Time_Created_At { get; set; }
		// achievemtn_templates
		public int Leaderboard_Rank { get; set; }
		public int Total_Leaderboard_Users { get; set; }
		// ftp object
		public string Device_Type_Display_Name { get; set; }
		// Ride object
		public bool Is_Skup_Intro_Available { get; set; }
		// total hr zones durations
		// average effort score
	}
}
