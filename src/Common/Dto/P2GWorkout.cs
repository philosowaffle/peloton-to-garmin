using Common.Dto.Peloton;

namespace Common.Dto
{
	public class P2GWorkout
	{
		private WorkoutType _workoutType;
		public WorkoutType WorkoutType => GetWorkoutType();

		public UserData UserData { get; set; }
		public Workout Workout { get; set; }
		public WorkoutSamples WorkoutSamples { get; set; }

		public dynamic Raw { get; set; }

		private WorkoutType GetWorkoutType()
		{
			if (_workoutType != WorkoutType.None) return _workoutType;
			if (Workout is null) return WorkoutType.None;

			return Workout.GetWorkoutType();
		}

		private bool IsOutdoorWorkout(WorkoutSamples workoutSamples)
		{
			if (workoutSamples == null) return false;

			return workoutSamples.Location_Data is object
				&& workoutSamples.Location_Data.Count > 0;
		}
	}

	public static class Extensions
	{
		public static WorkoutType GetWorkoutType(this Workout workout)
		{
			switch (workout.Fitness_Discipline)
			{
				case FitnessDiscipline.None: return WorkoutType.None;
				case FitnessDiscipline.Bike_Bootcamp: return WorkoutType.BikeBootcamp;
				case FitnessDiscipline.Caesar: return _workoutType = WorkoutType.Rowing; break
				case FitnessDiscipline.Cardio: return WorkoutType.Cardio;
				case FitnessDiscipline.Circuit: return WorkoutType.Circuit;
				case FitnessDiscipline.Cycling: return WorkoutType.Cycling;
				case FitnessDiscipline.Meditation: return WorkoutType.Meditation;
				case FitnessDiscipline.Strength: return WorkoutType.Strength;
				case FitnessDiscipline.Stretching: return WorkoutType.Stretching;
				case FitnessDiscipline.Yoga: return WorkoutType.Yoga;
				case FitnessDiscipline.Running when workout.Is_Outdoor: return WorkoutType.OutdoorRunning;
				case FitnessDiscipline.Running: return WorkoutType.TreadmillRunning;
				case FitnessDiscipline.Walking when workout.Is_Outdoor: return WorkoutType.OutdoorRunning;
				case FitnessDiscipline.Walking: return WorkoutType.TreadmillWalking;
				default: return WorkoutType.None;
			}
		}
	}
}
