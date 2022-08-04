namespace Common.Dto.Peloton;

public class UserData
{
	public string Cycling_Ftp_Source { get; set; }
	/// <summary>
	/// This is not truly the users FTP. This the max 20min avg output.
	/// You must calculate 95% of this to get the real FTP.
	/// </summary>
	public uint Cycling_Ftp { get; set; }
	/// <summary>
	/// This is not truly the users FTP. This the max 20min avg output.
	/// You must calculate 95% of this to get the real FTP.
	/// </summary>
	public uint Estimated_Cycling_Ftp { get; set; }

	public uint Default_Max_Heart_Rate { get; set; }
	public uint Customized_Max_Heart_Rate { get; set; }

	public uint Weight { get; set; }
}
