using NUnit.Framework;
using System;
using Common;
using FluentAssertions;
using System.IO;

namespace UnitTests.Common
{
	public class ConfigurationTests
	{
		[Test]
		public void DefaultConfigInit_App_ShouldHaveDefaulValues()
		{
			var config = new Settings();

			config.App.Should().NotBeNull();
			config.App.EnablePolling.Should().BeFalse();
			config.App.OutputDirectory.Should().Be(Path.Join(Environment.CurrentDirectory, "output"));
			config.App.WorkingDirectory.Should().Be(Path.Join(Environment.CurrentDirectory, "working"));
			config.App.PollingIntervalSeconds.Should().Be(86400);
			config.App.FailedDirectory.Should().Be(Path.Join(config.App.OutputDirectory, "failed"));
			config.App.DownloadDirectory.Should().Be(Path.Join(config.App.WorkingDirectory, "downloaded"));
			config.App.UploadDirectory.Should().Be(Path.Join(config.App.WorkingDirectory, "upload"));
		}

		[Test]
		public void DefaultConfigInit_Format_ShouldHaveDefaulValues()
		{
			var config = new Settings();

			config.Format.Should().NotBeNull();
			config.Format.Fit.Should().BeFalse();
			config.Format.Json.Should().BeFalse();
			config.Format.Tcx.Should().BeFalse();
			config.Format.SaveLocalCopy.Should().BeFalse();
			config.Format.IncludeTimeInHRZones.Should().BeFalse();
			config.Format.IncludeTimeInPowerZones.Should().BeFalse();
			config.Format.DeviceInfoPath.Should().BeNullOrEmpty();

			config.Format.Cycling.PreferredLapType.Should().Be(PreferredLapType.Default);
			config.Format.Running.PreferredLapType.Should().Be(PreferredLapType.Default);
		}

		[Test]
		public void DefaultConfigInit_Peloton_ShouldHaveDefaulValues()
		{
			var config = new Settings();

			config.Peloton.Should().NotBeNull();
			config.Peloton.Email.Should().BeNull();
			config.Peloton.Password.Should().BeNull();
			config.Peloton.NumWorkoutsToDownload.Should().Be(5);
			config.Peloton.ExcludeWorkoutTypes.Should().BeEmpty();
		}

		[Test]
		public void DefaultConfigInit_Garmin_ShouldHaveDefaulValues()
		{
			var config = new Settings();

			config.Garmin.Should().NotBeNull();
			config.Garmin.Email.Should().BeNull();
			config.Garmin.Password.Should().BeNull();
			config.Garmin.FormatToUpload.Should().Be(FileFormat.Fit);
			config.Garmin.Upload.Should().BeFalse();
			config.Garmin.UploadStrategy.Should().Be(UploadStrategy.NativeImplV1);
		}

		[Test]
		public void DefaultConfigInit_Observability_Prometheus_ShouldHaveDefaulValues()
		{
			var config = new AppConfiguration();

			config.Observability.Should().NotBeNull();
			config.Observability.Prometheus.Should().NotBeNull();
			config.Observability.Prometheus.Enabled.Should().BeFalse();
			config.Observability.Prometheus.Port.Should().BeNull();
		}

		[Test]
		public void DefaultConfigInit_Observability_Jaeger_ShouldHaveDefaulValues()
		{
			var config = new AppConfiguration();

			config.Observability.Should().NotBeNull();
			config.Observability.Jaeger.Should().NotBeNull();
			config.Observability.Jaeger.AgentHost.Should().BeNull();
			config.Observability.Jaeger.AgentPort.Should().BeNull();
		}

		[Test]
		public void DefaultConfigInit_Developer_ShouldHaveDefaulValues()
		{
			var config = new AppConfiguration();

			config.Developer.Should().NotBeNull();
			config.Developer.UserAgent.Should().BeNull();
		}
	}
}
