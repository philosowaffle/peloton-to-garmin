﻿using Api.Controllers;
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
}
