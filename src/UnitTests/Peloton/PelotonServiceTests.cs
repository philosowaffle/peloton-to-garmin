using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Service;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Peloton;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.Peloton
{
	public class PelotonServiceTests
	{
		[Test]
		public async Task GetRecentWorkoutsAsync_DoesNothing_WhenNoCount([Values(-1,0)]int numWorkoutsToDownload)
		{
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<PelotonService>();
			var pelotonApi = autoMocker.GetMock<IPelotonApi>();

			await pelotonService.GetRecentWorkoutsAsync(numWorkoutsToDownload);

			pelotonApi.Verify(x => x.GetWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
		}

		[Test]
		public async Task GetWorkoutDetailsAsync_Should_EnrichAllWorkouts()
		{
			// SETUP
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<PelotonService>();
			var pelotonApi = autoMocker.GetMock<IPelotonApi>();

			pelotonApi.Setup(x => x.GetWorkoutByIdAsync("1"))
					.ReturnsAsync(new JObject())
					.Verifiable();

			pelotonApi.Setup(x => x.GetWorkoutSamplesByIdAsync("1"))
					.ReturnsAsync(new JObject())
					.Verifiable();

			pelotonApi.Setup(x => x.GetWorkoutByIdAsync("2"))
					.ReturnsAsync(new JObject())
					.Verifiable();

			pelotonApi.Setup(x => x.GetWorkoutSamplesByIdAsync("2"))
					.ReturnsAsync(new JObject())
					.Verifiable();

			pelotonApi.Setup(x => x.GetWorkoutByIdAsync("3"))
					.ReturnsAsync(new JObject())
					.Verifiable();

			pelotonApi.Setup(x => x.GetWorkoutSamplesByIdAsync("3"))
					.ReturnsAsync(new JObject())
					.Verifiable();

			// ACT
			await pelotonService.GetWorkoutDetailsAsync(new List<Workout>()
					{
						new Workout() { Status = "COMPLETE", Id = "1" },
						new Workout() { Status = "COMPLETE", Id = "2" },
						new Workout() { Status = "COMPLETE", Id = "3" },
					});

			// ASSERT
			Mock.Verify();
			pelotonApi.Verify(x => x.GetWorkoutByIdAsync(It.IsAny<string>()), Times.Exactly(3));
			pelotonApi.Verify(x => x.GetWorkoutSamplesByIdAsync(It.IsAny<string>()), Times.Exactly(3));
		}

		[Test]
		public async Task GetRecentWorkoutsAsync_FetchesXNumberOfWorkoutsAcrossPages()
		{
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<PelotonService>();

			var pelotonApi = autoMocker.GetMock<IPelotonApi>();
			pelotonApi.Setup(x => x.GetWorkoutsAsync(2, 0)) // First call for data
				.ReturnsAsync(new PagedPelotonResponse<Workout>() { data = new List<Workout>() { new Workout() } });
			pelotonApi.Setup(x => x.GetWorkoutsAsync(1, 1)) // Second call for data
				.ReturnsAsync(new PagedPelotonResponse<Workout>() { data = new List<Workout>() { new Workout() } });

			var workouts = await pelotonService.GetRecentWorkoutsAsync(2);
			workouts.Result.Count.Should().Be(2);

			pelotonApi.Verify(x => x.GetWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
		}

		[Test]
		public async Task GetRecentWorkoutsAsync_StopsCallingPelotonWhenNoMoreWorkouts()
		{
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<PelotonService>();

			var pelotonApi = autoMocker.GetMock<IPelotonApi>();
			pelotonApi.Setup(x => x.GetWorkoutsAsync(20, 0)) // First call for data
				.ReturnsAsync(new PagedPelotonResponse<Workout>() { data = new List<Workout>() { new Workout() } });
			pelotonApi.Setup(x => x.GetWorkoutsAsync(19, 1)) // Second call for data
				.ReturnsAsync(new PagedPelotonResponse<Workout>() { data = new List<Workout>() { new Workout() } });
			pelotonApi.Setup(x => x.GetWorkoutsAsync(18, 2)) // Third call for data
				.ReturnsAsync(new PagedPelotonResponse<Workout>() { data = new List<Workout>() });

			var workouts = await pelotonService.GetRecentWorkoutsAsync(20);
			workouts.Result.Count.Should().Be(2);

			pelotonApi.Verify(x => x.GetWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
		}
	}

	public class InternalPelotonService : PelotonService
	{
		public InternalPelotonService(ISettingsService settingsService, IPelotonApi pelotonApi, IFileHandling fileHandler) : base(settingsService, pelotonApi, fileHandler)
		{
		}

		public new Task<ServiceResult<ICollection<Workout>>> GetRecentWorkoutsAsync(int numWorkoutsToDownload)
		{
			return base.GetRecentWorkoutsAsync(numWorkoutsToDownload);
		}
	}
}
