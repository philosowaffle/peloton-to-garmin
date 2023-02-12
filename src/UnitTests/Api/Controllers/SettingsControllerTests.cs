using Api.Controllers;
using Common;
using Common.Dto.Api;
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
	public async Task AppPost_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();

		var response = await controller.AppPost(null);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("PostRequest must not be null.");
	}

	[Test]
	public async Task AppPost_With_InvalidOutPutDir_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();

		fileHandler
			.Setup(f => f.DirExists("blah"))
			.Returns(false)
			.Verifiable();

		var request = new App() 
		{
			OutputDirectory = "blah"
		};

		var response = await controller.AppPost(request);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("Output Directory path is either not accessible or does not exist.");

		fileHandler.Verify();
	}

	[Test]
	public async Task AppPost_With_EmptyOutPutDir_DoesNotValidateIt()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		fileHandler
			.Setup(f => f.DirExists(It.IsAny<string>()))
			.Returns(false);

		var request = new App()
		{
			OutputDirectory = string.Empty
		};

		settingService
			.Setup(s => s.GetSettingsAsync())
			.ReturnsAsync(new Settings());

		var response = await controller.AppPost(request);

		var result = response.Result as OkObjectResult;
		result.Should().NotBeNull();

		fileHandler.Verify(f => f.DirExists(It.IsAny<string>()), Times.Never);
	}

	[Test]
	public async Task AppPost_With_EnablePollingWhenGarminMFAEnabled_Throws()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();
		var settingService = autoMocker.GetMock<ISettingsService>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();

		fileHandler
			.Setup(f => f.DirExists("blah"))
			.Returns(true)
			.Verifiable();

		settingService.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settingService.Object.GetSettingsAsync))
			.ReturnsAsync(new Settings() { Garmin = new() { TwoStepVerificationEnabled = true } });

		var request = new App()
		{
			EnablePolling = true,
			OutputDirectory = "blah",
		};

		var response = await controller.AppPost(request);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("Automatic Syncing cannot be enabled when Garmin TwoStepVerification is enabled.");
	}

	[Test]
	public async Task FormatPost_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();

		var response = await controller.FormatPost(null);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("PostRequest must not be null.");
	}

	[Test]
	public async Task FormatPost_With_InvalidDeviceInfoPath_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();

		fileHandler
			.Setup(f => f.FileExists("blah"))
			.Returns(false)
			.Verifiable();

		var request = new Format()
		{
			DeviceInfoPath = "blah"
		};

		var response = await controller.FormatPost(request);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("DeviceInfo path is either not accessible or does not exist.");

		fileHandler.Verify();
	}

	[Test]
	public async Task FormatPost_With_EmptyDeviceInfoDir_DoesNotValidateIt()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		var request = new Format()
		{
			DeviceInfoPath = string.Empty
		};

		settingService
			.Setup(s => s.GetSettingsAsync())
			.ReturnsAsync(new Settings());

		var response = await controller.FormatPost(request);

		var result = response.Result as OkObjectResult;
		result.Should().NotBeNull();

		fileHandler.Verify(f => f.DirExists(It.IsAny<string>()), Times.Never);
	}

	[Test]
	public async Task PelotonPost_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();

		var response = await controller.PelotonPost(null);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("PostRequest must not be null.");
	}

	[Test]
	public async Task PelotonPost_With_Invalid_NumWorkoutsToDownload_Returns400()
	{
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SettingsController>();

		var request = new SettingsPelotonPostRequest()
		{
			NumWorkoutsToDownload = -1
		};

		var response = await controller.PelotonPost(request);

		var result = response.Result as BadRequestObjectResult;
		result.Should().NotBeNull();
		var value = result.Value as ErrorResponse;
		value.Message.Should().Be("NumWorkoutsToDownload must be greater than 0.");
	}

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
