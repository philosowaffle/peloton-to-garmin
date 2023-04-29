using Api.Contract;
using Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System.Threading.Tasks;
using WebApp.Controllers;

namespace UnitTests.Api.Controllers;

public class SystemInfoControllerTests
{
	[Test]
	public async Task GetAsync_Calls_Service()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var controller = autoMocker.CreateInstance<SystemInfoController>();
		var service = autoMocker.GetMock<ISystemInfoService>();

		service.SetupWithAny<ISystemInfoService, Task<SystemInfoGetResponse>>(nameof(service.Object.GetAsync))
			.ReturnsAsync(new SystemInfoGetResponse());

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
		var response = actionResult.Result as OkObjectResult;
		response.Should().NotBeNull();

		service.Verify(x => x.GetAsync(request, "https", "localhost"), Times.Once());
	}
}
