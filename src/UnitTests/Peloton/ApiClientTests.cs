using Common;
using Common.Service;
using Common.Stateful;
using Flurl.Http.Testing;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Peloton;
using System.IO;
using System.Threading.Tasks;
using UnitTests.UnitTestHelpers;

namespace UnitTests.Peloton
{
	public class ApiClientTests
	{
		private string DataDirectory = Path.Join(FileHelper.DataDirectory, "peloton_responses");

		[Test]
		public async Task RowingWorkout_CanBe_Deserialized_To_Workout()
		{
			var autoMocker = new AutoMocker();
			var apiClient = autoMocker.CreateInstance<ApiClient>();
			var settingsService = autoMocker.GetMock<ISettingsService>();
			SetupSuccessfulAuth(settingsService);

			var responseData = await FileHelper.ReadTextFromFileAsync(Path.Join(DataDirectory, "rower_sample.json"));

			var httpMock = new HttpTest();
			httpMock
				.ForCallsTo("https://api.onepeloton.com/api/user/blah/workouts")
				.WithVerb("GET")
				.RespondWith(responseData, 200);

			await apiClient.GetWorkoutsAsync(5, 0);

			httpMock.ShouldHaveMadeACall();
		}

		[Test]
		public async Task RowingWorkout_CanBe_Deserialized_To_WorkoutSamples()
		{
			var autoMocker = new AutoMocker();
			var apiClient = autoMocker.CreateInstance<ApiClient>();
			var settingsService = autoMocker.GetMock<ISettingsService>();
			SetupSuccessfulAuth(settingsService);

			var responseData = await FileHelper.ReadTextFromFileAsync(Path.Join(DataDirectory, "rower_performance_graph.json"));

			var httpMock = new HttpTest();
			httpMock
				.ForCallsTo("https://api.onepeloton.com/api/workout/0/performance_graph")
				.WithVerb("GET")
				.RespondWith(responseData, 200);

			await apiClient.GetWorkoutSamplesByIdAsync("0");

			httpMock.ShouldHaveMadeACall();
		}

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
