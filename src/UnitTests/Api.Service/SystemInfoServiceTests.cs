using Api.Contract;
using Api.Service;
using Api.Services;
using Common;
using Common.Observe;
using Core.GitHub;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Philosowaffle.Capability.ReleaseChecks.Model;
using System;
using System.Threading.Tasks;

namespace UnitTests.Api.Service;

public class SystemInfoServiceTests
{
	[Test]
	public async Task GetAsync_WhenNotRequested_DoesNotEnrich_LatestVersionInfo()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SystemInfoService>();
		var ghService = autoMocker.GetMock<IGitHubReleaseCheckService>();
		Logging.InternalLevelSwitch = new Serilog.Core.LoggingLevelSwitch();

		var request = new SystemInfoGetRequest() { CheckForUpdate = false };

		// ACT
		var response = await controller.GetAsync(request, scheme: "https", host: "localhost");

		// ASSERT
		response.Should().NotBeNull();

		response.NewerVersionAvailable.Should().BeNull();
		response.LatestVersionInformation.Should().BeNull();

		ghService.Verify(x => x.GetLatestReleaseInformationAsync("philosowaffle", "peloton-to-garmin", Constants.AppVersion), Times.Never());
	}

	[Test]
	public async Task GetAsync_WhenRequest_Enriches_LatestVersionInfo()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SystemInfoService>();
		var ghService = autoMocker.GetMock<IVersionInformationService>();
		Logging.InternalLevelSwitch = new Serilog.Core.LoggingLevelSwitch();

		ghService.Setup(x => x.GetLatestReleaseInformationAsync())
			.ReturnsAsync(new LatestReleaseInformation()
			{
				IsReleaseNewerThanInstalledVersion = true,
				Description = "adf",
				LatestVersion = "v45.90.23",
				ReleaseDate = DateTime.Now,
				ReleaseUrl = "https://www.google.com"
			});

		// ACT
		var request = new SystemInfoGetRequest() { CheckForUpdate = true };
		var response = await controller.GetAsync(request, scheme: "https", host: "localhost");

		// ASSERT
		response.Should().NotBeNull();

		response.NewerVersionAvailable.Should().NotBeNull();
		response.LatestVersionInformation.Should().NotBeNull();

		ghService.Verify(x => x.GetLatestReleaseInformationAsync(), Times.Once());
	}
}
