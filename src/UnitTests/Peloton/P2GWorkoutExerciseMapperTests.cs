using NUnit.Framework;
using Peloton;
using System.Collections.Generic;
using Common.Dto.Peloton;
using FluentAssertions;

namespace UnitTests.PelotonTests;

public class P2GWorkoutExerciseMapperTests
{
	public static Workout[] Workouts =
	{
		null,
		new Workout(),
		new Workout() { Movement_Tracker_Data = new MovementTrackerData() },
		new Workout() { Movement_Tracker_Data = new MovementTrackerData() { Completed_Movements_Summary_Data = new CompletedMovementsSummaryData() } },
		new Workout() { Movement_Tracker_Data = new MovementTrackerData() { Completed_Movements_Summary_Data = new CompletedMovementsSummaryData() { Repetition_Summary_Data = new List<RepetitionSummaryData>() } } },
	};

	[Test, TestCaseSource(nameof(Workouts))]
	public void GetExercisesTrackedByMovementTracker_WhenNoData(Workout workout)
	{
		var exercises = P2GWorkoutExerciseMapper.GetExercisesTrackedByMovementTracker(workout);
		exercises.Should().NotBeNull();
		exercises.Should().BeEmpty();
	}

	[Test]
	public void GetExercisesTrackedByMovementTracker_WhenNoWeight()
	{
		var workout = new Workout()
		{
			Movement_Tracker_Data = new MovementTrackerData()
			{
				Completed_Movements_Summary_Data = new CompletedMovementsSummaryData()
				{
					Repetition_Summary_Data = new List<RepetitionSummaryData>()
					{
						new RepetitionSummaryData()
						{
							Weight = null
						},
						new RepetitionSummaryData()
						{
							Weight = new List<Weight>()
						},
						new RepetitionSummaryData()
						{
							Weight = new List<Weight>() { new Weight() }
						},
						new RepetitionSummaryData()
						{
							Weight = new List<Weight>() { new Weight() { Weight_Data = null } }
						},
						new RepetitionSummaryData()
						{
							Weight = new List<Weight>() { new Weight() { Weight_Data = new WeightData()
							{
								Weight_Unit = null,
							} } }
						},
					}
				}
			}
		};

		var exercises = P2GWorkoutExerciseMapper.GetExercisesTrackedByMovementTracker(workout);
		exercises.Should().NotBeNull();
		exercises.Should().HaveCount(5);
		exercises.Should().OnlyContain(e => e.Weight != null);
		exercises.Should().OnlyContain(e => e.Weight.Unit == null);
		exercises.Should().OnlyContain(e => e.Weight.Value == 0);
	}
}
