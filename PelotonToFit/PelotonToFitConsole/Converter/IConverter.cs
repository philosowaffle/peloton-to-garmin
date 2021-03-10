using Common;
using Peloton.Dto;
using System.Collections.Generic;

namespace PelotonToFitConsole.Converter
{
	public interface IConverter
	{
		public ConversionDetails Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary, Configuration config);
		public void Decode(string filePath);
	}

	public class ConversionDetails
	{
		public ConversionDetails()
		{
			Errors = new List<ConversionError>();
		}

		public bool Successful { get; set; }
		public string Path { get; set; }
		public string Name { get; set; }
		public ICollection<ConversionError> Errors { get; set; }

		public override string ToString()
		{
			return $"Name: {Name}, OutputPath: {Path}, Successful: {Successful}, Errors: \n {string.Join("\n", Errors)}";
		}
	}

	public class ConversionError
	{
		public string Message { get; set; }
		public string Details { get; set; }

		public override string ToString()
		{
			return $"Message: {Message}, Details: {Details}";
		}
	}
}
