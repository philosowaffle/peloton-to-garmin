using Common;
using Common.Dto;
using Conversion;
using Dynastream.Fit;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.Conversion
{
	public class FitConverterTests
	{
		private string DataDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data");
		private IOWrapper _fileHandler = new IOWrapper();

		[TestCase("cycling_workout", PreferredLapType.Default)]
		[TestCase("tread_run_workout", PreferredLapType.Default)]
		[TestCase("meditation_workout", PreferredLapType.Default)]
		[TestCase("walking_workout_01", PreferredLapType.Default)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Default)]

		[TestCase("cycling_workout", PreferredLapType.Distance)]
		[TestCase("tread_run_workout", PreferredLapType.Distance)]
		[TestCase("meditation_workout", PreferredLapType.Distance)]
		[TestCase("walking_workout_01", PreferredLapType.Distance)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Distance)]

		[TestCase("cycling_workout", PreferredLapType.Class_Segments)]
		[TestCase("tread_run_workout", PreferredLapType.Class_Segments)]
		[TestCase("meditation_workout", PreferredLapType.Class_Segments)]
		[TestCase("walking_workout_01", PreferredLapType.Class_Segments)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Class_Segments)]

		[TestCase("cycling_workout", PreferredLapType.Class_Targets)]
		[TestCase("tread_run_workout", PreferredLapType.Class_Targets)]
		[TestCase("meditation_workout", PreferredLapType.Class_Targets)]
		[TestCase("walking_workout_01", PreferredLapType.Class_Targets)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Class_Targets)]
		public void Fit_Converter_Creates_Valid_Fit(string filename, PreferredLapType lapType)
		{
			var workoutPath = Path.Join(DataDirectory, $"{filename}.json");
			var settings = new Settings()
			{
				Format = new Format()
				{
					Running = new Running()
					{
						PreferredLapType = lapType
					},
					Cycling = new Cycling()
					{
						PreferredLapType = lapType
					}
				}
			};
			var converter = new ConverterInstance(settings);
			var convertedMesgs = converter.ConvertForTest(workoutPath);

			convertedMesgs.Should().NotBeNullOrEmpty();

			var dest = Path.Join(DataDirectory, $"test_output_{filename}.fit");
			try
			{
				using (FileStream fitDest = new FileStream(dest, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
				{
					var validator = new Encode(ProtocolVersion.V20);
					validator.Open(fitDest);
					validator.Write(convertedMesgs); // validates while writing
					validator.Close();
				}
			} finally
			{
				System.IO.File.Delete(dest);
			}			
		}

		private class ConverterInstance : FitConverter
		{
			private IOWrapper fileHandler = new IOWrapper();

			public ConverterInstance() : base(new Settings(), null, null) { }

			public ConverterInstance(Settings settings) : base(settings, null, null) { }

			public ICollection<Mesg> ConvertForTest(string path)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = this.Convert(workoutData.Workout, workoutData.WorkoutSamples);

				return converted.Item2;
			}
		}
	}
}
