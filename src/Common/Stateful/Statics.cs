using System;

namespace Common.Stateful
{
	public static class Statics
	{
		public static string AppType = "unknown";
		public static string MetricPrefix = "p2g";
		public static string TracingService = "p2g";

		public static string DefaultDataDirectory = Environment.CurrentDirectory;
		public static string DefaultTempDirectory = Environment.CurrentDirectory;
		public static string DefaultOutputDirectory = Environment.CurrentDirectory;

		public static string ConfigPath;
	}
}
