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

		[TestCase("cycling_workout")]
		[TestCase("tread_run_workout")]
		[TestCase("meditation_workout")]
		public void Fit_Converter_Creates_Valid_Fit(string filename)
		{
			var workoutPath = Path.Join(DataDirectory, $"{filename}.json");
			var converter = new ConverterInstance();
			var convertedMesgs = converter.Convert(workoutPath);

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
			private IOWrapper _fileHandler = new IOWrapper();

			public ConverterInstance() : base(new Configuration(), null, null) { }

			public ICollection<Mesg> Convert(string path)
			{
				var workoutData = _fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = this.Convert(workoutData.Workout, workoutData.WorkoutSamples);

				return converted.Item2;
			}
		}
	}
}
