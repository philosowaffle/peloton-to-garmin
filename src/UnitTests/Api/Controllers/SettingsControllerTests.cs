using Api.Contract;
using Api.Controllers;
using Common;
using Common.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System.Threading.Tasks;

namespace UnitTests.Api.Controllers;

public class SettingsControllerTests
{
	[Test]
	public async Task GarminPost_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();

		var response = await controller.GarminPost(null);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("PostRequest must not be null.");
	}

	[Test]
	public async Task GarminPost_With_EnableGarminMFAWhenPollingEnabled_Throws()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		settingService.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settingService.Object.GetSettingsAsync))
			.ReturnsAsync(new Settings() { App = new() { EnablePolling = true } });

		SettingsGarminPostRequest request = new ()
		{
			Upload = true,
			TwoStepVerificationEnabled = true
		};

		var response = await controller.GarminPost(request);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("Garmin TwoStepVerification cannot be enabled while Automatic Syncing is enabled. Please disable Automatic Syncing first.");
	}
}
