using Api.Controllers;
using Common.Dto.Api;
using Common.Dto.Peloton;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Peloton;
using Peloton.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.Api.Controllers
{
	public class PelotonWorkoutsControllerTests
	{
		[Test]
		public async Task GetAsync_WithInvalid_PageSize_Returns400([Values(-1, 0)]int pageSize)
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();

			var request = new PelotonWorkoutsGetRequest() { PageSize = pageSize };

			var response = await controller.GetAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("PageSize must be greater than 0.");
		}

		[Test]
		public async Task GetAsync_WithInvalid_PageIndex_Returns400([Values(-1)] int pageIndex)
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();

			var request = new PelotonWorkoutsGetRequest() { PageIndex = pageIndex, PageSize = 5 };

			var response = await controller.GetAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("PageIndex must be greater than or equal to 0.");
		}

		[Test]
		public async Task GetAsync_Returns_Results_SortedBy_MostRecentFirst()
		{
			// setup
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();
			var service = autoMocker.GetMock<IPelotonService>();

			var results = new PagedPelotonResponse<Workout>()
			{
				Page = 0,
				Limit = 5,
				Count = 5,
				Page_Count = 3,
				Total = 15,
				data = new List<Workout>()
				{
					new Workout() { Created_At = DateTime.Now.AddMinutes(-15).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-3).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-1).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-25).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-8).Ticks },
				}
			};

			service.SetReturnsDefault(Task.FromResult(results));

			var request = new PelotonWorkoutsGetRequest() { PageIndex = 0, PageSize = 5 };

			// act
			var actionResult = await controller.GetAsync(request);

			// assert
			var response = actionResult.Value;
			response.Should().NotBeNull();

			response.Items.Should().BeInDescendingOrder(i => i.Created_At);
		}

		[Test]
		public async Task GetAsync_PageProperties_Should_Be_Mapped_Correctly()
		{
			// setup
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();
			var service = autoMocker.GetMock<IPelotonService>();

			var results = new PagedPelotonResponse<Workout>()
			{
				Page = 0,
				Limit = 5,
				Count = 5,
				Page_Count = 3,
				Total = 15,
				data = new List<Workout>()
				{
					new Workout() { Created_At = DateTime.Now.AddMinutes(-15).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-3).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-1).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-25).Ticks },
					new Workout() { Created_At = DateTime.Now.AddMinutes(-8).Ticks },
				}
			};

			service.SetReturnsDefault(Task.FromResult(results));

			var request = new PelotonWorkoutsGetRequest() { PageIndex = 0, PageSize = 5 };

			// act
			var actionResult = await controller.GetAsync(request);

			// assert
			var response = actionResult.Value;
			response.Should().NotBeNull();

			response.PageSize.Should().Be(results.Limit);
			response.PageIndex.Should().Be(results.Page);
			response.PageCount.Should().Be(results.Page_Count);
			response.TotalItems.Should().Be(results.Total);
			response.HasPrevious.Should().BeFalse();
			response.HasNext.Should().BeTrue();
		}

		[Test]
		public async Task GetAsync_When_Service_Throws_ArgumentException_Returns_BadRequest()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();
			var service = autoMocker.GetMock<IPelotonService>();

			service.Setup(s => s.GetPelotonWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()))
				.Throws(new ArgumentException("Some invalid param"));

			var request = new PelotonWorkoutsGetRequest() { PageSize = 5 };

			var response = await controller.GetAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Some invalid param");
		}

		[Test]
		public async Task GetAsync_When_Service_Throws_PelotonAuthenticationException_Returns_BadRequest()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();
			var service = autoMocker.GetMock<IPelotonService>();

			service.Setup(s => s.GetPelotonWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()))
				.Throws(new PelotonAuthenticationError("Peloton auth failed."));

			var request = new PelotonWorkoutsGetRequest() { PageSize = 5 };

			var response = await controller.GetAsync(request);

			var result = response.Result as BadRequestObjectResult;
			result.Should().NotBeNull();
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Peloton auth failed.");
		}

		[Test]
		public async Task GetAsync_When_Service_Throws_Exception_Returns_BadRequest()
		{
			var autoMocker = new AutoMocker();
			var controller = autoMocker.CreateInstance<PelotonWorkoutsController>();
			var service = autoMocker.GetMock<IPelotonService>();

			service.Setup(s => s.GetPelotonWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()))
				.Throws(new Exception("Some unhandled case."));

			var request = new PelotonWorkoutsGetRequest() { PageSize = 5 };

			var response = await controller.GetAsync(request);

			var result = response.Result as ObjectResult;
			result.Should().NotBeNull();
			result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
			var value = result.Value as ErrorResponse;
			value.Message.Should().Be("Unexpected error occurred: Some unhandled case.");
		}
	}
}
