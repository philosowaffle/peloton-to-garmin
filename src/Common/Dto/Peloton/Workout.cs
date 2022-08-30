using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.Dto.Peloton
{
	public class RecentWorkouts
	{
		public ICollection<RecentWorkout> data { get; set; }
	}

	public class RecentWorkout
	{
		public string Id { get; set; }
		public string Status { get; set; }
		public string Title { get; set; }
		public string Name { get; set; }
		public long Created_At { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public FitnessDiscipline Fitness_Discipline { get; set; }
		public Ride Ride { get; set; }
	}

	public class Workout
	{
		public long Created_At { get; set; }
		public string Device_Type { get; set; }
		public long? End_Time { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public FitnessDiscipline Fitness_Discipline { get; set; }
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
		public double Total_Work { get; set; }
		public string User_Id { get; set; }
		public int? V2_Total_Video_Watch_Time_Seconds { get; set; }
		public int? V2_Total_Video_Buffering_Seconds { get; set; }
		// User object
		public long Created { get; set; }
		public long Device_Time_Created_At { get; set; }
		// achievemtn_templates
		public int? Leaderboard_Rank { get; set; }
		public int Total_Leaderboard_Users { get; set; }
		public FTPInfo Ftp_Info { get; set; }
		public string Device_Type_Display_Name { get; set; }
		public Ride Ride { get; set; }
		public bool Is_Skip_Intro_Available { get; set; }
		// total hr zones durations
		// average effort score

	}

	public class FTPInfo
	{
		/// <summary>
		/// When Source == Ftp_Workout_Source then this is the true FTP
		/// When Source == Ftp_Manual_Source then calculate FTP x .95
		///  - This is not truly the users FTP. This the max 20min avg output.
		/// </summary>
		public ushort Ftp { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public CyclingFtpSource? Ftp_Source { get; set; }
		public string Ftp_Workout_Id { get; set; }
	}

	public enum FitnessDiscipline
	{
		None = 0,
		Cycling = 1,
		Bike_Bootcamp = 2,
		Running = 3,
		Walking = 4,
		Cardio = 5,
		Circuit = 6,
		Strength = 7,
		Stretching = 8,
		Yoga = 9,
		Meditation = 10
	}
}
