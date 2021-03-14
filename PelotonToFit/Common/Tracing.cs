using System;
using System.Diagnostics;

namespace Common
{
	public static class Tracing
	{
		public static ActivitySource Source;

		public static bool ValidateConfig(ObservabilityConfig config)
		{
			if (!config.Jaeger.Enabled)
				return true;

			if (string.IsNullOrEmpty(config.Jaeger.AgentHost))
			{
				Console.Out.WriteLine($"Agent Host must be set: {nameof(config)}.{nameof(config.Jaeger.AgentHost)}.");
				return false;
			}

			if (config.Jaeger.AgentPort is null || config.Jaeger.AgentPort <= 0)
			{
				Console.Out.WriteLine($"Agent Port must be set: {nameof(config)}.{nameof(config.Jaeger.AgentPort)}.");
				return false;
			}

			return true;
		}
	}
}
