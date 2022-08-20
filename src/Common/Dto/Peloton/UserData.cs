using System.Text.Json.Serialization;

namespace Common.Dto.Peloton;

public class UserData
{
	/// <summary>
	/// When Source == Ftp_Workout_Source then use Cycling_Workout_Ftp
	/// When Source == Ftp_Manual_Source then use Cycling_Ftp
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public CyclingFtpSource Cycling_Ftp_Source { get; set; }
	/// <summary>
	/// Actual FTP generated from a workout
	/// </summary>
	public ushort Cycling_Workout_Ftp { get; set; }
	public string Cycling_Ftp_Workout_Id { get; set; }


	/// <summary>
	/// This is not truly the users FTP. This the max 20min avg output.
	/// You must calculate 95% of this to get the real FTP.
	/// </summary>
	public ushort Cycling_Ftp { get; set; }

	public ushort Estimated_Cycling_Ftp { get; set; }

	public uint Default_Max_Heart_Rate { get; set; }
	public uint Customized_Max_Heart_Rate { get; set; }

	public uint Weight { get; set; }
}

public enum CyclingFtpSource
{
	Unknown = 0,
	Ftp_Workout_Source = 1,
	Ftp_Manual_Source = 2,
	Ftp_Estimated_Source = 3
}
