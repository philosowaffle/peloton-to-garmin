using Common.Dto.Peloton;
using System.IO;

namespace Common.Helpers;

public static class WorkoutHelper
{
	public const char SpaceSeparator = '_';

	private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();	

	public static string GetTitle(Workout workout)
	{
		var rideTitle = workout.Ride?.Title ?? workout.Id;
		var instructorName = workout.Ride?.Instructor?.Name;

		if (instructorName is object)
			instructorName = $" with {instructorName}";

		var title = $"{rideTitle}{instructorName}"
			.Replace(' ', SpaceSeparator);

		foreach (var c in InvalidFileNameChars)
		{
			title = title.Replace(c, '-');
		}

		return title;
	}

	public static string GetUniqueTitle(Workout workout)
	{
		return $"{workout.Id}_{GetTitle(workout)}";
	}

	public static string GetWorkoutIdFromFileName(string filePath)
	{
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		var parts = fileName.Split("_");
		return parts[0];
	}
}
