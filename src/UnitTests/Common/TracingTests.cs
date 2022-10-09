using Common;
using Common.Observe;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace UnitTests.Common
{
    public class TracingTests
	{
		[Test]
		public void ValidateConfig_InvalidPort_Throws([Values(-1, 0)] int port)
		{
			var config = new Observability();
			config.Jaeger.Enabled = true;
			config.Jaeger.AgentPort = port;
			config.Jaeger.AgentHost = "host";

			Action action = () => Tracing.ValidateConfig(config);
			action.Should().Throw<ArgumentException>();
		}

		[Test]
		public void ValidateConfig_ValidHostPort_DoesNotThrow()
		{
			var config = new Observability();
			config.Jaeger.Enabled = true;
			config.Jaeger.AgentPort = 8000;
			config.Jaeger.AgentHost = "host";

			Tracing.ValidateConfig(config);
		}

		[Test]
		public void ValidateConfig_InvalidHost_Throws([Values(null, "")] string agentHost)
		{
			var config = new Observability();
			config.Jaeger.Enabled = true;
			config.Jaeger.AgentPort = 8000;
			config.Jaeger.AgentHost = agentHost;

			Action action = () => Tracing.ValidateConfig(config);
			action.Should().Throw<ArgumentException>();
		}

		[Test]
		public void ValidateConfig_Disabled_DoesNotThrow()
		{
			var config = new Observability();
			config.Jaeger.Enabled = false;

			Tracing.ValidateConfig(config);
		}
	}
}
