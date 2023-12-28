using Common.Dto;
using Common.Dto.Peloton;
using System.IO;

namespace Common.Helpers;

public static class WorkoutHelper
{
	public const char SpaceSeparator = '_';

	private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();	

	public static string GetTitle(Workout workout, Format settings)
	{
		var rideTitle = workout.Ride?.Title ?? workout.Id;
		var instructorName = workout.Ride?.Instructor?.Name;
		var prefix = settings.WorkoutTitlePrefix ?? string.Empty;

		if (instructorName is object)
			instructorName = $" with {instructorName}";

		var title = $"{prefix}{rideTitle}{instructorName}"
			.Replace(' ', SpaceSeparator);

		foreach (var c in InvalidFileNameChars)
		{
			title = title.Replace(c, '-');
		}

		return title;
	}

	public static string GetUniqueTitle(Workout workout, Format settings)
	{
		return $"{workout.Id}_{GetTitle(workout, settings)}";
	}

	public static string GetWorkoutIdFromFileName(string filePath)
	{
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		var parts = fileName.Split("_");
		return parts[0];
	}
}
