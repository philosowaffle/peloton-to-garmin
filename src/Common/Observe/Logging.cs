using Serilog;

namespace Common.Observe
{
    public class LogContext
    {
        public static ILogger ForClass<T>() => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).Name);
		public static ILogger ForStatic(string name) => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, name);
	}
}
