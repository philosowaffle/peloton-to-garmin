using System.Text.Json.Serialization;

namespace Common.Dto.Peloton;

public record UserData
{
	/// <summary>
	/// When Source == Ftp_Workout_Source then use Cycling_Workout_Ftp
	/// When Source == Ftp_Manual_Source then use Cycling_Ftp
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public CyclingFtpSource? Cycling_Ftp_Source { get; init; }
	/// <summary>
	/// Actual FTP generated from a workout
	/// </summary>
	public ushort Cycling_Workout_Ftp { get; init; }
	public string Cycling_Ftp_Workout_Id { get; init; }


	/// <summary>
	/// This is not truly the users FTP. This the max 20min avg output.
	/// You must calculate 95% of this to get the real FTP.
	/// </summary>
	public ushort Cycling_Ftp { get; init; }

	public ushort Estimated_Cycling_Ftp { get; init; }

	public uint Default_Max_Heart_Rate { get; init; }
	public uint Customized_Max_Heart_Rate { get; init; }

	public uint Weight { get; init; }
}

public enum CyclingFtpSource : byte
{
	Unknown = 0,
	Ftp_Workout_Source = 1,
	Ftp_Manual_Source = 2,
	Ftp_Estimated_Source = 3
}
