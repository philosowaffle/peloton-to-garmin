using Common.Dto.Peloton;
using Common.Dto;
using System.Collections.Generic;
using System.Linq;

namespace Peloton;

public static class P2GWorkoutExerciseMapper
{
	public static ICollection<P2GExercise> GetWorkoutExercises(Workout workout, RideSegments rideSegments)
	{
		var exercises = GetExercisesTrackedByMovementTracker(workout);

		var segments = rideSegments?.Segments?.Segment_List;
		if (segments is null || segments.Count <= 0) return exercises;

		foreach (var segment in segments)
		{
			if (segment?.SubSegments_V2 is null || segment.SubSegments_V2.Count <= 0) continue;

			foreach (var subSegment in segment.SubSegments_V2)
			{
				if (subSegment?.Movements is null || subSegment.Movements.Count <= 0) continue;

				foreach (var movement in subSegment.Movements)
				{
					if (movement is null) continue;

					if (exercises.Any(e => e.StartOffsetSeconds == subSegment.Offset && e.Id == movement.Id))
						continue; // we likely already have this movement accounted for from the TrackedMovements

					var exercise = new P2GExercise()
					{
						Id = movement.Id,
						Name = movement.Name,
						Type = MovementTargetType.Time,
						StartOffsetSeconds = subSegment.Offset.GetValueOrDefault(),
						DurationSeconds = subSegment.Length.GetValueOrDefault() / subSegment.Movements.Count, // if this is a group of movements, divide up the time per movement
						Reps = subSegment.Rounds
					};
					exercises.Add(exercise);
				}
			}
		}

		var sorted = exercises.OrderBy(e => e.StartOffsetSeconds).ToList();
		return sorted;
	}

	public static ICollection<P2GExercise> GetExercisesTrackedByMovementTracker(Workout workout)
	{
		var movements = new List<P2GExercise>();

		var trackedRepData = workout?.Movement_Tracker_Data?.Completed_Movements_Summary_Data?.Repetition_Summary_Data;
		if (trackedRepData is not null && trackedRepData.Count > 0)
		{
			foreach (var repData in trackedRepData)
			{
				MovementTargetType movementTargetType = MovementTargetType.Reps;
				if (repData.Tracking_Type == TrackingTypes.TimeBased 
					|| repData.Tracking_Type == TrackingTypes.TimeTrackedRep
					|| repData.Tracking_Type == TrackingTypes.Rounds)
				{
					movementTargetType = MovementTargetType.Time;
				}

				var movement = new P2GExercise()
				{
					Id = repData.Movement_Id,
					Name = repData.Movement_Name,
					Type = movementTargetType,
					StartOffsetSeconds = repData.Offset,
					DurationSeconds = repData.Length,
					Reps = repData.Completed_Reps,
					Weight = new P2GWeight()
					{
						Unit = repData?.Weight?.FirstOrDefault()?.Weight_Data?.Weight_Unit,
						Value = repData?.Weight?.FirstOrDefault()?.Weight_Data?.Weight_Value ?? 0
					}
				};
				movements.Add(movement);
			}
		}

		return movements;
	}
}
