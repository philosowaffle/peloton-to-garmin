using Common;
using Common.Dto;
using Common.Helpers;
using Conversion;
using Dynastream.Fit;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Peloton;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitTests
{
	public class AdHocTests
	{
		private string DataDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data");

		[OneTimeSetUp]
		public void Setup()
		{
			Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.MinimumLevel.Verbose()
					.CreateLogger();
		}

		//[Test]
		//public void DecodeFitFile()
		//{
		//	var syncMyWorkoutFitFile = Path.Join(DataDirectory, "Fenix_Incline.fit");
		//	FitDecoder.Decode(syncMyWorkoutFitFile);
		//}

		//[Test]
		//public async Task DownloadWorkout()
		//{
		//	var email = "";
		//	var password = "";

		//var workoutId = "aaa6527fd5a74b7e8e2f8975c6025e60";

		//	var client = new ApiClient(email, password, false);
		//	await client.InitAuthAsync();

		//	var workout = await client.GetWorkoutByIdAsync(workoutId);
		//	var workoutSamples = await client.GetWorkoutSamplesByIdAsync(workoutId);
		//	var workoutSummary = await client.GetWorkoutSummaryByIdAsync(workoutId);

		//	dynamic data = new JObject();
		//	data.Workout = workout;
		//	data.WorkoutSamples = workoutSamples;
		//	data.WorkoutSummary = workoutSummary;

		//	Log.Debug(data.ToString());
		//	SaveRawData(data, workoutId, DataDirectory);
		//}

		//[Test]
		//public async Task DeSerialize()
		//{
		//	var file = Path.Join(DataDirectory, "test.json");
		//	var _fileHandler = new IOWrapper();
		//	var workout = _fileHandler.DeserializeJson<P2GWorkout>(file);
		//}

		//[Test]
		//public async Task Convert()
		//{
		//	var file = Path.Join(DataDirectory, "running_workout_01.json");

		//	var autoMocker = new AutoMocker();
		//	var settings = new Settings()
		//	{
		//		Format = new Format()
		//		{
		//			Running = new Running()
		//			{
		//				PreferredLapType = PreferredLapType.Distance
		//			}
		//		}
		//	};

		//	var fitConverter = new ConverterInstance(settings);
		//	var messages = fitConverter.ConvertForTest(file);
		//}

		private void SaveRawData(dynamic data, string workoutId, string path)
		{
			System.IO.File.WriteAllText(Path.Join(path, $"{workoutId}_workout.json"), data.ToString());
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
