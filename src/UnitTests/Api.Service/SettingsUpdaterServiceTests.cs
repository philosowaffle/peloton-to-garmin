using Api.Contract;
using Api.Service;
using Api.Service.Helpers;
using Common;
using Common.Dto;
using Common.Service;
using FluentAssertions;
using Garmin.Auth;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System.Threading.Tasks;

namespace UnitTests.Api.Service;

public class SettingsUpdaterServiceTests
{
	[Test]
	public async Task UpdateAppSettingsAsync_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();

		var response = await service.UpdateAppSettingsAsync(null);

		response.IsErrored().Should().BeTrue();
		response.Error.Should().NotBeNull();
		response.Error.Message.Should().Be("Updated AppSettings must not be null or empty.");
	}

	[Test]
	public async Task UpdatePelotonSettingsAsync_With_NullRequest_ReturnsError()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();

		var response = await service.UpdatePelotonSettingsAsync(null);

		response.IsErrored().Should().BeTrue();
		response.Error.Should().NotBeNull();
		response.Error.Message.Should().Be("Updated PelotonSettings must not be null or empty.");
	}

	[Test]
	public async Task UpdatePelotonSettingsAsync_With_Invalid_NumWorkoutsToDownload_And_PollingEnabled_ReturnsError()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		settingService.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settingService.Object.GetSettingsAsync))
			.ReturnsAsync(new Settings() { App = new() { EnablePolling = true } });

		var request = new SettingsPelotonPostRequest()
		{
			NumWorkoutsToDownload = -1
		};

		var response = await service.UpdatePelotonSettingsAsync(request);

		response.IsErrored().Should().BeTrue();
		response.Error.Should().NotBeNull();
		response.Error.Message.Should().Be("Number of workouts to download must but greater than 0 when Automatic Polling is enabled.");
	}

	[Test]
	public async Task FormatPost_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();

		var response = await service.UpdateFormatSettingsAsync(null);

		response.IsErrored().Should().BeTrue();
		response.Error.Should().NotBeNull();
		response.Error.Message.Should().Be("Updated Format Settings must not be null or empty.");
	}

	[Test]
	public async Task FormatPost_With_InvalidDeviceInfoPath_Returns400()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();

		fileHandler
			.Setup(f => f.FileExists("blah"))
			.Returns(false)
			.Verifiable();

		var request = new Format()
		{
			DeviceInfoPath = "blah"
		};

		var response = await service.UpdateFormatSettingsAsync(request);

		response.IsErrored().Should().BeTrue();
		response.Error.Should().NotBeNull();
		response.Error.Message.Should().Be("The DeviceInfo path is either not accessible or does not exist.");

		fileHandler.Verify();
	}

	[Test]
	public async Task FormatPost_With_EmptyDeviceInfoDir_DoesNotValidateIt()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();
		var fileHandler = autoMocker.GetMock<IFileHandling>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		var request = new Format()
		{
			DeviceInfoPath = string.Empty
		};

		settingService
			.Setup(s => s.GetSettingsAsync())
			.ReturnsAsync(new Settings());

		var response = await service.UpdateFormatSettingsAsync(request);

		response.IsErrored().Should().BeFalse();
		response.Result.Should().NotBeNull();

		fileHandler.Verify(f => f.DirExists(It.IsAny<string>()), Times.Never);
	}

	[Test]
	public async Task GarminPost_With_NullRequest_Returns400()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();

		var response = await service.UpdateGarminSettingsAsync(null);

		response.IsErrored().Should().BeTrue();
		response.Error.Should().NotBeNull();
		response.Error.Message.Should().Be("Updated Garmin Settings must not be null or empty.");
	}

	[Test]
	public async Task GarminPost_With_EmailChange_Should_SignOut_of_Garmin()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		settingService
			.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settingService.Object.GetSettingsAsync))
			.ReturnsAsync(new Settings() 
			{ 
				App = new() { EnablePolling = true },
				Garmin = new () { Email = "ogEmail", Password = "ogPassword" }
			});

		SettingsGarminPostRequest request = new()
		{
			Email = "newEmail",
		};

		var response = await service.UpdateGarminSettingsAsync(request);

		autoMocker
			.GetMock<IGarminAuthenticationService>()
			.Verify(x => x.SignOutAsync(), Times.Once);

		response.IsErrored().Should().BeFalse();
		response.Error.Should().BeNull();
		response.Successful.Should().BeTrue();
	}

	[Test]
	public async Task GarminPost_With_PasswordChange_Should_SignOut_of_Garmin()
	{
		var autoMocker = new AutoMocker();
		var service = autoMocker.CreateInstance<SettingsUpdaterService>();
		var settingService = autoMocker.GetMock<ISettingsService>();

		settingService
			.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settingService.Object.GetSettingsAsync))
			.ReturnsAsync(new Settings()
			{
				App = new() { EnablePolling = true },
				Garmin = new() { Email = "ogEmail", Password = "ogPassword" }
			});

		SettingsGarminPostRequest request = new()
		{
			Password = "newPassword",
		};

		var response = await service.UpdateGarminSettingsAsync(request);

		autoMocker
			.GetMock<IGarminAuthenticationService>()
			.Verify(x => x.SignOutAsync(), Times.Once);

		response.IsErrored().Should().BeFalse();
		response.Error.Should().BeNull();
		response.Successful.Should().BeTrue();
	}
}
