using Common;
using Common.Database;
using Common.Dto.Peloton;
using FluentAssertions;
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
	public class PelotonServiceTests
	{
		[Test]
		public async Task DownloadLatestWorkoutDataAsync_DoesNothing_WhenNoCount([Values(-1,0)]int numWorkoutsToDownload)
		{
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<PelotonService>();
			var pelotonApi = autoMocker.GetMock<IPelotonApi>();

			await pelotonService.DownloadLatestWorkoutDataAsync(numWorkoutsToDownload);

			pelotonApi.Verify(x => x.InitAuthAsync(It.IsAny<string>()), Times.Never);
		}

		[Test]
		public async Task GetRecentWorkoutsAsync_FetchesXNumberOfWorkoutsAcrossPages()
		{
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<InternalPelotonService>();

			var pelotonApi = autoMocker.GetMock<IPelotonApi>();
			pelotonApi.Setup(x => x.GetWorkoutsAsync(2, 0)) // First call for data
				.ReturnsAsync(new RecentWorkouts() { data = new List<RecentWorkout>() { new RecentWorkout() } });
			pelotonApi.Setup(x => x.GetWorkoutsAsync(1, 1)) // Second call for data
				.ReturnsAsync(new RecentWorkouts() { data = new List<RecentWorkout>() { new RecentWorkout() } });

			var workouts = await pelotonService.GetRecentWorkoutsAsync(2);
			workouts.Count.Should().Be(2);

			pelotonApi.Verify(x => x.GetWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
		}

		[Test]
		public async Task GetRecentWorkoutsAsync_StopsCallingPelotonWhenNoMoreWorkouts()
		{
			var autoMocker = new AutoMocker();
			var pelotonService = autoMocker.CreateInstance<InternalPelotonService>();

			var pelotonApi = autoMocker.GetMock<IPelotonApi>();
			pelotonApi.Setup(x => x.GetWorkoutsAsync(20, 0)) // First call for data
				.ReturnsAsync(new RecentWorkouts() { data = new List<RecentWorkout>() { new RecentWorkout() } });
			pelotonApi.Setup(x => x.GetWorkoutsAsync(19, 1)) // Second call for data
				.ReturnsAsync(new RecentWorkouts() { data = new List<RecentWorkout>() { new RecentWorkout() } });
			pelotonApi.Setup(x => x.GetWorkoutsAsync(18, 2)) // Third call for data
				.ReturnsAsync(new RecentWorkouts() { data = new List<RecentWorkout>() });

			var workouts = await pelotonService.GetRecentWorkoutsAsync(20);
			workouts.Count.Should().Be(2);

			pelotonApi.Verify(x => x.GetWorkoutsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
		}
	}

	public class InternalPelotonService : PelotonService
	{
		public InternalPelotonService(Settings config, IPelotonApi pelotonApi, IDbClient dbClient, IFileHandling fileHandler) : base(config, pelotonApi, dbClient, fileHandler)
		{
		}

		public new Task<List<RecentWorkout>> GetRecentWorkoutsAsync(int numWorkoutsToDownload)
		{
			return base.GetRecentWorkoutsAsync(numWorkoutsToDownload);
		}
	}
}
