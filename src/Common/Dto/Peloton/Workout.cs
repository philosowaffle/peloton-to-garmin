using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.Dto.Peloton
{
	public record PagedPelotonResponse<T> : PelotonResponse<T>
	{
		public ushort Limit { get; init; }
		public ushort Page { get; init; }
		public ushort Page_Count { get; init; }
	}

	public record PelotonResponse<T>
	{
		public ushort Total { get; init; }
		public ushort Count { get; init; }
		public ICollection<T> data { get; init; }
	}

	public record Workout
	{
		public string Id { get; init; }
		public string Status { get; init; }
		public string Title { get; init; }
		public string Name { get; init; }
		public long Created_At { get; init; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public FitnessDiscipline Fitness_Discipline { get; init; }
		public bool Is_Outdoor { get; init; }
		public Ride Ride { get; init; }

		//public string Device_Type { get; init; }
		public long? End_Time { get; init; }
		//public bool Has_Pedaling_Metrics { get; init; }
		//public bool Has_Leaderboard_Metrics { get; init; }
		//public bool Is_ToTal_Work_Personal_Record { get; init; }
		//public string Metrics_Type { get; init; }
		//public string Peloton_Id { get; init; }
		//public string Platform { get; init; }
		public long Start_Time { get; init; }
		//public string Strava_Id { get; init; }
		//public string Timezone { get; init; }
		public double Total_Work { get; init; }
		//public string User_Id { get; init; }
		//public int? V2_Total_Video_Watch_Time_Seconds { get; init; }
		//public int? V2_Total_Video_Buffering_Seconds { get; init; }
		// User object
		//public long Created { get; init; }
		//public long Device_Time_Created_At { get; init; }
		// achievemtn_templates
		//public int? Leaderboard_Rank { get; init; }
		//public int Total_Leaderboard_Users { get; init; }
		public FTPInfo Ftp_Info { get; init; }
		//public string Device_Type_Display_Name { get; init; }
		//public bool Is_Skip_Intro_Available { get; init; }
		// total hr zones durations
		// average effort score

		public MovementTrackerData Movement_Tracker_Data { get; init; }

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

	public enum FitnessDiscipline : byte
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
		Meditation = 10,
		Caesar = 11, // Project Caesar = Rower
	}
}
