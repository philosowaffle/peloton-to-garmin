using Common.Dto.Peloton;

namespace Common.Dto
{
	public class P2GWorkout
	{
		public WorkoutType WorkoutType => GetWorkoutType();

		public UserData UserData { get; set; }
		public Workout Workout { get; set; }
		public WorkoutSamples WorkoutSamples { get; set; }

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
	}
}
