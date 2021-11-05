using Serilog;

namespace Common
{
	public class LogContext 
	{
		public static ILogger ForClass<T>() => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).Name);
	}
}
