namespace Common.Dto
{
	public class GarminDeviceInfo
	{
		public string Name { get; set; }
		public uint UnitId { get; set; }
		public ushort ProductID { get; set; }
		public ushort ManufacturerId { get; set; } = 1;
		public GarminDeviceVersion Version { get; set; }
	}

	public class GarminDeviceVersion
	{
		public int VersionMajor { get; set; }
		public double VersionMinor { get; set; }
		public int BuildMajor { get; set; }
		public double BuildMinor { get; set; }
	}
}
