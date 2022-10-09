using Common.Dto;
using Common.Dto.Peloton;
using Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace UnitTests.Common
{
	public class WorkoutHelperTests
	{
		[Test]
		public void GetTitle_ShouldReturn_RideTitle()
		{
			var workout = new Workout()
			{
				Ride = new Ride()
				{
					Title = "My Title",
					Instructor = new Instructor()
					{
						Name = "My Name"
					}
				}
			};

			var title = WorkoutHelper.GetTitle(workout);
			title.Should().Be("My_Title_with_My_Name");
		}

		[Test]
		public void GetTitle_NullRide_ShouldReturn_RideId()
		{
			var workout = new Workout()
			{
				Id = "someId"
			};

			var title = WorkoutHelper.GetTitle(workout);
			title.Should().Be("someId");
		}

		[Test]
		public void GetTitle_NullInstructor_ShouldReturn_EmptyInstructorName()
		{
			var workout = new Workout()
			{
				Ride = new Ride()
				{
					Title = "My Title"
				}
			};

			var title = WorkoutHelper.GetTitle(workout);
			title.Should().Be("My_Title");
		}

		[Test]
		public void GetTitle_NullInstructorName_ShouldReturn_EmptyInstructorName()
		{
			var workout = new Workout()
			{
				Ride = new Ride()
				{
					Title = "My Title",
					Instructor = new Instructor()
				}
			};

			var title = WorkoutHelper.GetTitle(workout);
			title.Should().Be("My_Title");
		}
	}
}
