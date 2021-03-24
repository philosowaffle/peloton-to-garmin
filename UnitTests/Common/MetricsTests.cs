using Common;
using NUnit.Framework;
using FluentAssertions;
using System;

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
		public void ValidateConfig_ValidPort_ReturnsTrue()
		{
			var config = new Observability();
			config.Prometheus.Enabled = true;
			config.Prometheus.Port = 8000;

			Metrics.ValidateConfig(config).Should().BeTrue();
		}

		[Test]
		public void ValidateConfig_Disabled_ReturnsTrue()
		{
			var config = new Observability();
			config.Prometheus.Enabled = false;

			Metrics.ValidateConfig(config).Should().BeTrue();
		}
	}
}
