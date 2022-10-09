using Common.Dto.Api;
using FluentAssertions;
using GitHub;
using GitHub.Dto;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using WebApp.Controllers;

namespace UnitTests.Api.Controllers;

public class SystemInfoControllerTests
{
	[Test]
	public async Task GetAsync_WhenNotRequest_DoesNotEnrich_LatestVersionInfo()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SystemInfoController>();
		var ghService = autoMocker.GetMock<IGitHubService>();

		var context = new DefaultHttpContext();
		context.Request.Scheme = "https";
		context.Request.Host = new HostString("localhost");
		controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext()
		{
			HttpContext = context
		};

		var request = new SystemInfoGetRequest() { CheckForUpdate = false };

		// ACT
		var actionResult = await controller.GetAsync(request);

		// ASSERT
		var response = actionResult.Value;
		response.Should().NotBeNull();

		response.NewerVersionAvailable.Should().BeNull();
		response.LatestVersionInformation.Should().BeNull();

		ghService.Verify(x => x.GetLatestReleaseAsync(), Times.Never());
	}

	[Test]
	public async Task GetAsync_WhenRequest_Enriches_LatestVersionInfo()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SystemInfoController>();
		var ghService = autoMocker.GetMock<IGitHubService>();

		var context = new DefaultHttpContext();
		context.Request.Scheme = "https";
		context.Request.Host = new HostString("localhost");
		controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext()
		{
			HttpContext = context
		};

		ghService.Setup(x => x.GetLatestReleaseAsync())
			.ReturnsAsync(new P2GLatestRelease()
			{
				IsReleaseNewerThanInstalledVersion = true, 
				Description = "adf",
				LatestVersion = "v45.90.23",
				ReleaseDate = DateTime.Now,
				ReleaseUrl = "https://www.google.com"
			});

		// ACT
		var request = new SystemInfoGetRequest() { CheckForUpdate = true };
		var actionResult = await controller.GetAsync(request);

		// ASSERT
		var response = actionResult.Value;
		response.Should().NotBeNull();

		response.NewerVersionAvailable.Should().NotBeNull();
		response.LatestVersionInformation.Should().NotBeNull();

		ghService.Verify(x => x.GetLatestReleaseAsync(), Times.Once());
	}
}
