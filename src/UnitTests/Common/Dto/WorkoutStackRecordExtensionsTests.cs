using Common.Dto.P2G;
using Common.Dto.Peloton;
using Common.Dto;
using System.Collections.Generic;
using NUnit.Framework;
using FluentAssertions;
using System.Linq;

namespace UnitTests.Common.Dto;

public class WorkoutStackRecordExtensionsTests
{
	[Test]
	public void TryFindWorkoutStackRecord_ShouldReturnTrue_WhenWorkoutExists()
	{
		// Arrange
		var workout = new P2GWorkout { Workout = new Workout { Id = "workout1" } };
		var stackRecord = new WorkoutStackRecord
		{
			Workouts = new List<StackedWorkout>
				{
					new StackedWorkout { Id = "workout1" }
				}
		};
		var records = new List<WorkoutStackRecord> { stackRecord };

		// Act
		var result = records.TryFindWorkoutStackThisWorkoutBelongsTo(workout, out var foundRecord);

		// Assert
		result.Should().BeTrue();
		foundRecord.Should().NotBeNull();
		foundRecord.Should().Be(stackRecord);
	}

	[Test]
	public void TryFindWorkoutStackRecord_ShouldReturnFalse_WhenWorkoutDoesNotExist()
	{
		// Arrange
		var workout = new P2GWorkout { Workout = new Workout { Id = "workout2" } };
		var stackRecord = new WorkoutStackRecord
		{
			Workouts = new List<StackedWorkout>
				{
					new StackedWorkout { Id = "workout1" }
				}
		};
		var records = new List<WorkoutStackRecord> { stackRecord };

		// Act
		var result = records.TryFindWorkoutStackThisWorkoutBelongsTo(workout, out var foundRecord);

		// Assert
		result.Should().BeFalse();
		foundRecord.Should().BeNull();
	}

	[Test]
	public void IsStackComplete_ShouldReturnTrue_WhenStackIsComplete()
	{
		// Arrange
		var workout = new P2GWorkout
		{
			StackedWorkouts = new HashSet<P2GWorkout>
				{
					new P2GWorkout { Workout = new Workout { Id = "workout1" } },
					new P2GWorkout { Workout = new Workout { Id = "workout2" } }
				}
		};
		var stackRecord = new WorkoutStackRecord
		{
			Workouts = new List<StackedWorkout>
				{
					new StackedWorkout { Id = "workout1" },
					new StackedWorkout { Id = "workout2" }
				}
		};
		var records = new List<WorkoutStackRecord> { stackRecord };

		// Act
		var result = workout.IsStackComplete(records);

		// Assert
		result.Should().BeTrue();
	}

	[Test]
	public void IsStackComplete_ShouldReturnFalse_WhenStackIsIncomplete()
	{
		// Arrange
		var workout = new P2GWorkout
		{
			StackedWorkouts = new HashSet<P2GWorkout>
				{
					new P2GWorkout { Workout = new Workout { Id = "workout1" } }
				}
		};
		var stackRecord = new WorkoutStackRecord
		{
			Workouts = new List<StackedWorkout>
				{
					new StackedWorkout { Id = "workout1" },
					new StackedWorkout { Id = "workout2" }
				}
		};
		var records = new List<WorkoutStackRecord> { stackRecord };

		// Act
		var result = workout.IsStackComplete(records);

		// Assert
		result.Should().BeFalse();
	}

	[Test]
	public void CreateNewStacksIfNeeded_ShouldCreateNewStacks_WhenNewStackIsFound()
	{
		// Arrange
		var newWorkout = new P2GWorkout
		{
			StackedWorkouts = new HashSet<P2GWorkout>
				{
					new P2GWorkout { Workout = new Workout { Id = "workout1" } },
					new P2GWorkout { Workout = new Workout { Id = "workout2" } }
				}
		};
		var existingStack = new WorkoutStackRecord
		{
			Workouts = new List<StackedWorkout>
				{
					new StackedWorkout { Id = "workout3" }
				}
		};
		var previouslySeenStacks = new List<WorkoutStackRecord> { existingStack };

		// Act
		var result = new List<P2GWorkout> { newWorkout }.CreateNewStacksIfNeeded(previouslySeenStacks);

		// Assert
		result.Should().NotBeNullOrEmpty();
		result.Should().HaveCount(1);
		result.Should().AllBeOfType<WorkoutStackRecord>();
		result.First().Id.Should().Be("workout1_workout2");
	}

	[Test]
	public void CreateNewStacksIfNeeded_ShouldNotCreateNewStacks_WhenStackAlreadyExists()
	{
		// Arrange
		var newWorkout = new P2GWorkout
		{
			StackedWorkouts = new HashSet<P2GWorkout>
				{
					new P2GWorkout { Workout = new Workout { Id = "workout1" } }
				}
		};
		var existingStack = new WorkoutStackRecord
		{
			Workouts = new List<StackedWorkout>
				{
					new StackedWorkout { Id = "workout1" }
				}
		};
		var previouslySeenStacks = new List<WorkoutStackRecord> { existingStack };

		// Act
		var result = new List<P2GWorkout> { newWorkout }.CreateNewStacksIfNeeded(previouslySeenStacks);

		// Assert
		result.Should().BeEmpty();
	}
}
