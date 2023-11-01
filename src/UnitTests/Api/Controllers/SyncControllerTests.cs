using Api.Controllers;
using Common;
using Common.Dto.Api;
using Common.Service;
using Common.Stateful;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErrorResponse = Common.Dto.Api.ErrorResponse;
using SyncErrorResponse = Sync.ErrorResponse;

namespace UnitTests.Api.Controllers
{
	public class SyncControllerTests
	{
		[Test]
		public async Task SyncAsync_With_NullRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();

			var response = await controller.SyncAsync(null);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("PostRequest must not be null.");
		}

		[Test]
		public async Task SyncAsync_With_DefaultRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			var request = new SyncPostRequest();

			var response = await controller.SyncAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("WorkoutIds must not be empty.");
		}

		[Test]
		public async Task SyncAsync_With_EmptyWorkoutIdsRequest_Returns400()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() };

			var response = await controller.SyncAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("WorkoutIds must not be empty.");
		}

		[Test]
		public async Task SyncAsync_WhenGarminMfaEnabled_AndNoAuthTokenYet_Returns401()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings() { Garmin = new() { Upload = true, TwoStepVerificationEnabled = true } });

			settings.SetupWithAny<ISettingsService, GarminApiAuthentication>(nameof(settings.Object.GetGarminAuthentication))
				.Returns((GarminApiAuthentication)null);

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() { "someId" } };

			var response = await controller.SyncAsync(request);

			var result = response.Result as UnauthorizedObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Must initialize Garmin two factor auth token before sync can be preformed.");
			value.Code.Should().Be(ErrorCode.NeedToInitGarminMFAAuth);
		}

		[Test]
		public async Task SyncAsync_WorkoutIds_Calls_CorrectMethod()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			service.SetReturnsDefault(Task.FromResult(new SyncResult() { SyncSuccess = true }));

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() { "someId" } };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as CreatedResult;
			response.Should().NotBeNull();

			service.Verify(s => s.SyncAsync(It.IsAny<ICollection<string>>(), null), Times.Once);
		}

		[Test]
		public async Task SyncAsync_When_Service_Throws_Exception_Returns_BadRequest()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			service.Setup(s => s.SyncAsync(It.IsAny<ICollection<string>>(), null))
				.Throws(new Exception("Some unhandled case."));

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() { "someId" } };

			var actionResult = await controller.SyncAsync(request);

			var result = actionResult.Result as ObjectResult;
			result.Should().NotBeNull();
			result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Unexpected error occurred: Some unhandled case.");
		}

		[Test]
		public async Task SyncAsync_When_SyncUnsuccessful_OkResult_Returned()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			service.SetReturnsDefault(Task.FromResult(new SyncResult() { SyncSuccess = false }));

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() { "someId" } };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as OkObjectResult;
			response.Should().NotBeNull();

			var result = response.Value as SyncPostResponse;
			result.SyncSuccess.Should().BeFalse();
		}

		[Test]
		public async Task SyncAsync_When_SyncSuccessful_CreatedResult_Returned()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			service.SetReturnsDefault(Task.FromResult(new SyncResult()
			{
				SyncSuccess = true,
				PelotonDownloadSuccess = true,
				ConversionSuccess = true,
				UploadToGarminSuccess = true
			}));

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() { "someId" } };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as CreatedResult;
			response.Should().NotBeNull();

			var result = response.Value as SyncPostResponse;
			result.SyncSuccess.Should().BeTrue();
			result.PelotonDownloadSuccess.Should().BeTrue();
			result.ConverToFitSuccess.Should().BeTrue();
			result.UploadToGarminSuccess.Should().BeTrue();
			result.Errors.Should().BeNullOrEmpty();
		}

		[Test]
		public async Task SyncAsync_When_SyncErrors_MapsErrorsCorrectly()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<SyncController>();
			var service = autoMocker.GetMock<ISyncService>();
			var settings = autoMocker.GetMock<ISettingsService>();

			settings.SetupWithAny<ISettingsService, Task<Settings>>(nameof(settings.Object.GetSettingsAsync))
				.ReturnsAsync(new Settings());

			var syncResult = new SyncResult()
			{
				SyncSuccess = false,
			};
			syncResult.Errors.Add(new SyncErrorResponse() { Message = "error 1" });
			syncResult.Errors.Add(new SyncErrorResponse() { Message = "error 2" });
			syncResult.Errors.Add(new SyncErrorResponse() { Message = "error 3" });
			service.SetReturnsDefault(Task.FromResult(syncResult));

			var request = new SyncPostRequest() { WorkoutIds = new List<string>() { "someId" } };

			var actionResult = await controller.SyncAsync(request);

			var response = actionResult.Result as OkObjectResult;
			response.Should().NotBeNull();

			var result = response.Value as SyncPostResponse;
			result.SyncSuccess.Should().BeFalse();
			result.Errors.Should().NotBeNullOrEmpty();
			result.Errors.Count.Should().Be(3);
			result.Errors.ElementAt(0).Message.Should().Be("error 1");
			result.Errors.ElementAt(1).Message.Should().Be("error 2");
			result.Errors.ElementAt(2).Message.Should().Be("error 3");
		}
	}
}
