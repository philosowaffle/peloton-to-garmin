using System;

namespace Common.Dto;

public static class SystemInformation
{
	public static string RunTimeVersion = Environment.Version.ToString();
	public static string OS = Environment.OSVersion.Platform.ToString();
	public static string OSVersion = Environment.OSVersion.VersionString;
	public static bool RunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
}
