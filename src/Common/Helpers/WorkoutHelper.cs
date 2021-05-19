using Common.Dto;

namespace Common.Helpers
{
	public static class WorkoutHelper
	{
		public static string GetTitle(Workout workout)
		{
			var rideTitle = workout.Ride?.Title ?? workout.Id;
			var instructorName = workout.Ride?.Instructor?.Name;

			if (instructorName is object)
				instructorName = $" with {instructorName}";

			return $"{rideTitle}{instructorName}"
				.Replace(" ", "_")
				.Replace("/", "-")
				.Replace(":", "-");
		}

		public static string GetUniqueTitle(Workout workout)
		{
			return $"{workout.Id}_{GetTitle(workout)}";
		}
	}
}
