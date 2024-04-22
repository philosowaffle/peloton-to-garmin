using Common.Dto;
using Common.Dto.Peloton;
using HandlebarsDotNet;
using System.IO;
using System.Web;

namespace Common.Helpers;

public static class WorkoutHelper
{
	public const char SpaceSeparator = '_';
	public const char InvalidCharacterReplacer = '-';
	public const char Space = ' ';

	private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

	public static string GetTitle(Workout workout, Format settings)
	{
		var rideTitle = workout.Ride?.Title ?? workout.Id;
		var instructorName = workout.Ride?.Instructor?.Name;

		var templateData = new 
		{
			PelotonWorkoutTitle = rideTitle,
			PelotonInstructorName = instructorName
		};

		var template = settings.WorkoutTitleTemplate;
		if (string.IsNullOrWhiteSpace(template))
			template = new Format().WorkoutTitleTemplate;
		
		var compiledTemplate = Handlebars.Compile(settings.WorkoutTitleTemplate);
		var title = compiledTemplate(templateData);

		var cleanedTitle = title.Replace(Space, SpaceSeparator);

		foreach (var c in InvalidFileNameChars)
		{
			cleanedTitle = cleanedTitle.Replace(c, InvalidCharacterReplacer);
		}

		var result = HttpUtility.HtmlDecode(cleanedTitle);
		return result;
	}

	public static string GetUniqueTitle(Workout workout, Format settings)
	{
		return $"{workout.Id}_{GetTitle(workout, settings)}";
	}

	public static string GetWorkoutIdFromFileName(string filePath)
	{
		var fileName = Path.GetFileNameWithoutExtension(filePath);
		var parts = fileName.Split(SpaceSeparator);
		return parts[0];
	}
}
