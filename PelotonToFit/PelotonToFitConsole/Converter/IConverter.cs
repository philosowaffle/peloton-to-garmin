using Peloton.Dto;
using System.Collections.Generic;

namespace PelotonToFitConsole.Converter
{
	public interface IConverter
	{
		public ConversionDetails Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary);
	}

	public class ConversionDetails
	{
		public string Path { get; set; }
		public object Data { get; set; }
		public ICollection<ConversionError> Errors { get; set; }
	}

	public class ConversionError
	{
		public string Message { get; set; }
		public string Details { get; set; }
	}
}
