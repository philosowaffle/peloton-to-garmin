namespace Common.Dto.Peloton;

public class UserData
{
	public string Cycling_Ftp_Source { get; set; }
	public uint Cycling_Ftp { get; set; }
	public uint Estimated_Cycling_Ftp { get; set; }

	public uint Default_Max_Heart_Rate { get; set; }
	public uint Customized_Max_Heart_Rate { get; set; }

	public uint Weight { get; set; }
}
