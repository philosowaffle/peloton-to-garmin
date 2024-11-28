namespace Common.Dto.Garmin
{
	public class GarminDeviceInfo
	{
		public string Name { get; set; }
		public uint UnitId { get; set; }
		public ushort ProductID { get; set; }
		public ushort ManufacturerId { get; set; } = 1;
		public GarminDeviceVersion Version { get; set; } = new ();
	}

	public class GarminDeviceVersion
	{
		public int VersionMajor { get; set; }
		public double VersionMinor { get; set; }
		public int BuildMajor { get; set; }
		public double BuildMinor { get; set; }
	}

	public static class GarminDevices
	{
		public static readonly GarminDeviceInfo TACXDevice = new GarminDeviceInfo()
		{
			Name = "TacxTrainingAppWin", // Max 20 Chars
			ProductID = 20533, // GarminProduct.TacxTrainingAppWin,
			UnitId = 1,
			ManufacturerId = 89, // Tacx
			Version = new GarminDeviceVersion()
			{
				VersionMajor = 1,
				VersionMinor = 30,
				BuildMajor = 0,
				BuildMinor = 0,
			}
		};

		public static readonly GarminDeviceInfo EpixDevice = new GarminDeviceInfo()
		{
			Name = "Epix", // Max 20 Chars
			ProductID = 3943, // GarminProduct.EpixGen2,
			UnitId = 3413684246,
			ManufacturerId = 1, // Garmin
			Version = new GarminDeviceVersion()
			{
				VersionMajor = 10,
				VersionMinor = 43,
				BuildMajor = 0,
				BuildMinor = 0,
			}
		};

		public static readonly GarminDeviceInfo Forerunner945 = new GarminDeviceInfo()
		{
			Name = "Forerunner 945", // Max 20 Chars
			ProductID = 3113, // GarminProduct.Fr945,
			UnitId = 1,
			ManufacturerId = 1, // Garmin
			Version = new GarminDeviceVersion()
			{
				VersionMajor = 19,
				VersionMinor = 2,
				BuildMajor = 0,
				BuildMinor = 0,
			}
		};
	}
}
