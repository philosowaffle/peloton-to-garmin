using Common.Dto;
using Common.Dto.Peloton;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.Common.Dto;

public class P2GWorkoutTests
{
	[Test]
	public void GetWorkoutType_Should_Map_Correctly([Values]FitnessDiscipline fitnessDiscipline, [Values] bool isOutdoor) 
	{
		var workout = new P2GWorkout()
		{
			Workout = new Workout() { Fitness_Discipline = fitnessDiscipline, Is_Outdoor = isOutdoor },
			WorkoutSamples = new WorkoutSamples() { Location_Data = isOutdoor ? new List<LocationData>() { new LocationData() } : null }
		};

		var workoutType = workout.WorkoutType;

			switch (fitnessDiscipline)
			{
				case FitnessDiscipline.None: workoutType.Should().Be(WorkoutType.None); break;
				case FitnessDiscipline.Bike_Bootcamp: workoutType.Should().Be(WorkoutType.BikeBootcamp); break;
				case FitnessDiscipline.Cardio: workoutType.Should().Be(WorkoutType.Cardio); break;
				case FitnessDiscipline.Caesar: workoutType.Should().Be(WorkoutType.Rowing); break;
				case FitnessDiscipline.Circuit: workoutType.Should().Be(WorkoutType.Circuit); break;
				case FitnessDiscipline.Cycling: workoutType.Should().Be(WorkoutType.Cycling); break;
				case FitnessDiscipline.Meditation: workoutType.Should().Be(WorkoutType.Meditation); break;
				case FitnessDiscipline.Strength: workoutType.Should().Be(WorkoutType.Strength); break;
				case FitnessDiscipline.Stretching: workoutType.Should().Be(WorkoutType.Stretching); break;
				case FitnessDiscipline.Yoga: workoutType.Should().Be(WorkoutType.Yoga); break;
				case FitnessDiscipline.Running when isOutdoor: workoutType.Should().Be(WorkoutType.OutdoorRunning); break;
				case FitnessDiscipline.Running: workoutType.Should().Be(WorkoutType.TreadmillRunning); break;
				case FitnessDiscipline.Walking when isOutdoor: workoutType.Should().Be(WorkoutType.OutdoorWalking); break;
				case FitnessDiscipline.Walking: workoutType.Should().Be(WorkoutType.TreadmillWalking); break;
			}
		}

}
