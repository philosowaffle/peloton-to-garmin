using Common;
using Common.Dto.Peloton;
using Common.Service;
using Common.Stateful;
using FluentAssertions;
using Flurl.Http.Testing;
using Microsoft.OpenApi.Expressions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Peloton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Peloton
{
	public class ApiClientTests
	{
		//[Test]
		//public async Task AA()
		//{
		//	// Some Http Test examples
		//	var autoMocker = new AutoMocker();
		//	var apiClient = autoMocker.CreateInstance<ApiClient>();
		//	var settingsService = autoMocker.GetMock<ISettingsService>();
		//	SetupSuccessfulAuth(settingsService);

		//	var httpMock = new HttpTest();
		//	httpMock
		//		.ForCallsTo("https://api.onepeloton.com/api/me")
		//		.WithVerb("GET")
		//		.RespondWithJson("{\"cycling_ftp_source\": null }", 500);

		//	httpMock.Settings.JsonSerializer.Deserialize<UserData>("{\"cycling_ftp_source\": null }");

		//	var response = await apiClient.GetUserDataAsync();

		//	httpMock.ShouldHaveCalled("https://api.onepeloton.com/api/me");
		//	response.Cycling_Ftp_Source.Should().Be(CyclingFtpSource.Unknown);

			

		//	httpMock.ShouldHaveMadeACall();
		//}

		private void SetupSuccessfulAuth(Mock<ISettingsService> settingsService)
		{
			settingsService.Setup(x => x.GetSettingsAsync()).ReturnsAsync(new Settings()
			{
				Peloton = new()
				{
					Email = "blah",
					Password = "blah"
				}
			});

			settingsService.Setup(x => x.GetPelotonApiAuthentication(It.IsAny<string>())).Returns(new PelotonApiAuthentication()
			{
				Email = "blah",
				Password = "blah",
				UserId = "blah",
				SessionId = "blah"
			});
		}
	}
}
