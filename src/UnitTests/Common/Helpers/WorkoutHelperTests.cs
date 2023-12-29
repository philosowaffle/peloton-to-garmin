using Common.Dto;
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

		return WorkoutHelper.GetTitle(workout, new Format());
	}

	[Test]
	public void GetTitle_NullRide_ShouldReturn_RideId()
	{
		var workout = new Workout()
		{
			Id = "someId"
		};

		var title = WorkoutHelper.GetTitle(workout, new Format());
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

		var title = WorkoutHelper.GetTitle(workout, new Format());
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

		var title = WorkoutHelper.GetTitle(workout, new Format());
		title.Should().Be("My_Title");
	}

	[Test]
	public void GetTitle_NullPrefix_ShouldReturn_EmptyPrefix()
	{
		var workout = new Workout()
		{
			Ride = new Ride()
			{
				Title = "My Title",
				Instructor = new Instructor() { Name = "Instructor"}
			}
		};

		var title = WorkoutHelper.GetTitle(workout, new Format());
		title.Should().Be("My_Title_with_Instructor");
	}

	[Test]
	public void GetTitle_With_Prefix_ShouldReturn_Title_With_Prefix()
	{
		var format = new Format()
		{
			WorkoutTitlePrefix = "Peloton - "
		};

		var workout = new Workout()
		{
			Ride = new Ride()
			{
				Title = "My Title",
				Instructor = new Instructor() { Name = "Instructor" }
			}
		};

		var title = WorkoutHelper.GetTitle(workout, format);
		title.Should().Be("Peloton_-_My_Title_with_Instructor");
	}
}
