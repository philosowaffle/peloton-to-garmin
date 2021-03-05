
namespace PelotonToFitConsole
{
	public static class Configuration
	{
		public static Severity DebugSeverity { get; set; }
	}

	public enum Severity
	{
		None,
		Info,
		Debug
	}
}
