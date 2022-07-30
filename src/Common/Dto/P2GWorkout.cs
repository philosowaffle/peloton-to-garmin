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

			var isOutdoorWorkout = IsOutdoorWorkout(WorkoutSamples);

			switch (Workout.Fitness_Discipline)
			{
				case FitnessDiscipline.None: _workoutType = WorkoutType.None; break;
				case FitnessDiscipline.Bike_Bootcamp: _workoutType = WorkoutType.BikeBootcamp; break;
				case FitnessDiscipline.Cardio: _workoutType = WorkoutType.Cardio; break;
				case FitnessDiscipline.Circuit: _workoutType = WorkoutType.Circuit; break;
				case FitnessDiscipline.Cycling: _workoutType = WorkoutType.Cycling; break;
				case FitnessDiscipline.Meditation: _workoutType = WorkoutType.Meditation; break;
				case FitnessDiscipline.Strength: _workoutType = WorkoutType.Strength; break;
				case FitnessDiscipline.Stretching: _workoutType = WorkoutType.Stretching; break;
				case FitnessDiscipline.Yoga: _workoutType = WorkoutType.Yoga; break;
				case FitnessDiscipline.Running when isOutdoorWorkout: _workoutType = WorkoutType.OutdoorRunning; break;
				case FitnessDiscipline.Running: _workoutType = WorkoutType.TreadmillRunning; break;
				case FitnessDiscipline.Walking when isOutdoorWorkout: _workoutType = WorkoutType.OutdoorRunning; break;
				case FitnessDiscipline.Walking: _workoutType = WorkoutType.TreadmillWalking; break;
			}

			return _workoutType;
		}

		private bool IsOutdoorWorkout(WorkoutSamples workoutSamples)
		{
			if (workoutSamples == null) return false;

			return workoutSamples.Location_Data is object
				&& workoutSamples.Location_Data.Count > 0;
		}
	}
}
