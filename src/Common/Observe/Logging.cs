using Common.Dto;
using Common.Stateful;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.File;
using Serilog.Sinks.File.Header;
using System.IO;
using System.Text;

namespace Common.Observe;

public class LogContext
{
	public static ILogger ForClass<T>() => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).Name);
	public static ILogger ForStatic(string name) => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, name);
}

public static class Logging
{
	public static string CurrentFilePath { get; set; }
	public static LoggingLevelSwitch InternalLevelSwitch { get; set; }

	public static void LogSystemInformation()
	{
		Log.Information("*********************************************");
		Log.Information("P2G Version: {@AppName} {@Version}", Statics.AppType, Constants.AppVersion);
		Log.Information("Operating System: {@Os}", SystemInformation.OS);
		Log.Information("OS Version: {@OsVersion}", SystemInformation.OSVersion);
		Log.Information("DotNet Runtime: {@DotnetRuntime}", SystemInformation.RunTimeVersion);
		Log.Information("Docker Deployment: {@IsDocker}", SystemInformation.RunningInDocker);
		Log.Information("Config path: {@ConfigPath}", Statics.ConfigPath);
		Log.Information("*********************************************");
	}

	public static string GetSystemInformationLogMessage()
	{
		return $@"
*********************************************
P2G Version: {Statics.AppType} {Constants.AppVersion}
Operating System: {SystemInformation.OS}
OS Version: {SystemInformation.OSVersion}
DotNet Runtime: {SystemInformation.RunTimeVersion}
Docker Deployment: {SystemInformation.RunningInDocker}
Config path: {Statics.ConfigPath}
*********************************************";
	}
}

public class CaptureFilePathHook : FileLifecycleHooks
{
	private HeaderWriter _headerHook;

	public CaptureFilePathHook()
	{
		_headerHook = new HeaderWriter(Logging.GetSystemInformationLogMessage);
	}

	public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
	{
		Logging.CurrentFilePath = path;

		return _headerHook.OnFileOpened(path, underlyingStream, encoding);
	}

	public override void OnFileDeleting(string path)
	{
		_headerHook.OnFileDeleting(path);
	}
}
