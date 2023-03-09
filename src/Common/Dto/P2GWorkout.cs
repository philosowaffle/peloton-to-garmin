using Common.Dto.Peloton;
using System.Collections.Generic;
using System.Linq;

namespace Common.Dto
{
	public class P2GWorkout
	{
		public WorkoutType WorkoutType => GetWorkoutType();

		public UserData UserData { get; set; }
		public Workout Workout { get; set; }
		public WorkoutSamples WorkoutSamples { get; set; }
		public RideDetails RideDetails { get; set; }
		public ICollection<P2GExercise> Exercises { get; set; }

		public dynamic Raw { get; set; }

		private WorkoutType GetWorkoutType()
		{
			if (Workout is null) return WorkoutType.None;

			return Workout.GetWorkoutType();
		}
	}

	public static class Extensions
	{
		public static WorkoutType GetWorkoutType(this Workout workout)
		{
			return workout.Fitness_Discipline switch
			{
				FitnessDiscipline.None => WorkoutType.None,
				FitnessDiscipline.Bike_Bootcamp => WorkoutType.BikeBootcamp,
				FitnessDiscipline.Caesar => WorkoutType.Rowing,
				FitnessDiscipline.Cardio => WorkoutType.Cardio,
				FitnessDiscipline.Circuit => WorkoutType.Circuit,
				FitnessDiscipline.Cycling => WorkoutType.Cycling,
				FitnessDiscipline.Meditation => WorkoutType.Meditation,
				FitnessDiscipline.Strength => WorkoutType.Strength,
				FitnessDiscipline.Stretching => WorkoutType.Stretching,
				FitnessDiscipline.Yoga => WorkoutType.Yoga,
				FitnessDiscipline.Running when workout.Is_Outdoor => WorkoutType.OutdoorRunning,
				FitnessDiscipline.Running => WorkoutType.TreadmillRunning,
				FitnessDiscipline.Walking when workout.Is_Outdoor => WorkoutType.OutdoorWalking,
				FitnessDiscipline.Walking => WorkoutType.TreadmillWalking,
				_ => WorkoutType.None,
			};
		}

		public static ICollection<P2GExercise> GetClassPlanExercises(Workout workout, Segments rideSegments)
		{
			var movements = new List<P2GExercise>();

			var trackedRepData = workout?.Movement_Tracker_Data?.Completed_Movements_Summary_Data?.Repetition_Summary_Data;
			if (trackedRepData is not null && trackedRepData.Count > 0)
			{
				foreach (var repData in trackedRepData)
				{
					var movement = new P2GExercise()
					{
						Id = repData.Movement_Id,
						Name = repData.Movement_Name,
						Type = repData.Is_Hold ? MovementTargetType.Time : MovementTargetType.Reps,
						StartOffsetSeconds = repData.Offset,
						DurationSeconds = repData.Length,
						Reps = repData.Completed_Number,
						Weight = new P2GWeight()
						{
							Unit = repData?.Weight?.FirstOrDefault()?.Weight_Data?.Weight_Unit,
							Value = repData?.Weight?.FirstOrDefault().Weight_Data?.Weight_Value ?? 0
						}
					};
					movements.Add(movement);
				}

				return movements;
			}

			var segments = rideSegments?.Segment_List;
			if (segments is null || segments.Count <= 0) return movements;

			foreach (var segment in segments)
			{
				if (segment.SubSegments_V2 is null || segment.SubSegments_V2.Count <= 0) continue;
				foreach (var subSegment in segment.SubSegments_V2)
				{
					var mov = subSegment.Movements?.FirstOrDefault();
					if (mov is null) continue;
					var movement = new P2GExercise()
					{
						Id = mov.Id,
						Name = mov.Name,
						Type = MovementTargetType.Time,
						StartOffsetSeconds = subSegment.Offset.GetValueOrDefault(),
						DurationSeconds = subSegment.Length.GetValueOrDefault(),
						Reps = subSegment.Rounds
					};
					movements.Add(movement);
				}
			}

			return movements;
		}
	}

	public record P2GExercise
	{
		public string Id { get; init; }
		public string Name { get; init; }
		public int StartOffsetSeconds { get; init; }
		public int DurationSeconds { get; init; }
		public MovementTargetType Type { get; init; }
		public int? Reps { get; init; }
		public P2GWeight? Weight { get; init; }
	}

	public record P2GWeight
	{
		public double Value { get; init; }

		/// <summary>
		/// lb
		/// </summary>
		public string Unit { get; init; }
	}

	public enum MovementTargetType : byte
	{
		Unknown = 0,
		Reps = 1,
		Time = 2
	}
}
