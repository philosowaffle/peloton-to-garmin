using System;
using System.IO;

namespace Common.Stateful
{
	public static class Statics
	{
		public static string AppType = "unknown";
		public static string MetricPrefix = "p2g";
		public static string TracingService = "p2g";

		public static string DefaultConfigDirectory = Environment.GetEnvironmentVariable($"{Constants.EnvironmentVariablePrefix}_CONFIG_DIRECTORY") ?? Environment.CurrentDirectory;

		public static string DefaultDataDirectory = Path.GetFullPath(Path.Join(Environment.CurrentDirectory, "data"));
		public static string DefaultTempDirectory = Path.Join(Environment.CurrentDirectory, "working");
		public static string DefaultOutputDirectory = Path.Join(Environment.CurrentDirectory, "output");

		public static string ConfigPath;
	}
}
