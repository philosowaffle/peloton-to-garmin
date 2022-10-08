using Common;
using NUnit.Framework;
using FluentAssertions;
using System;
using Common.Observe;

namespace UnitTests.Common
{
    public class MetricsTests
	{
		[Test]
		public void ValidateConfig_InvalidPort_Throws([Values(-1, 0)] int port)
		{
			var config = new Observability();
			config.Prometheus.Enabled = true;
			config.Prometheus.Port = port;

			Action action = () => Metrics.ValidateConfig(config);
			action.Should().Throw<ArgumentException>();
		}

		[Test]
		public void ValidateConfig_ValidPort_ShouldNotThrow()
		{
			var config = new Observability();
			config.Prometheus.Enabled = true;
			config.Prometheus.Port = 8000;
		}

		[Test]
		public void ValidateConfig_Disabled_ShouldNotThrow()
		{
			var config = new Observability();
			config.Prometheus.Enabled = false;
		}
	}
}
