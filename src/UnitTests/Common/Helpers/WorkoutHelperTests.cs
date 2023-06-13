using Common.Dto.Peloton;
using Common.Helpers;
using FluentAssertions;
using NUnit.Framework;

namespace UnitTests.Common;

public class WorkoutHelperTests
{

	[Platform(Exclude ="Unix,Linux,MacOSX")]
	[TestCase("My Title", "Some Instructor", ExpectedResult = "My_Title_with_Some_Instructor")]
	[TestCase("My/Title", "Some/Instructor", ExpectedResult = "My-Title_with_Some-Instructor")]
	[TestCase("My:Title", "Some:Instructor", ExpectedResult = "My-Title_with_Some-Instructor")]
	[TestCase("My*Title", "Some*Instructor", ExpectedResult = "My-Title_with_Some-Instructor")]
	public string GetTitle_Should_ReplaceInvalidChars(string title, string instructor)
	{
		var workout = new Workout()
		{
			Ride = new Ride()
			{
				Title = title,
				Instructor = new Instructor()
				{
					Name =instructor
				}
			}
		};

		return WorkoutHelper.GetTitle(workout);
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
