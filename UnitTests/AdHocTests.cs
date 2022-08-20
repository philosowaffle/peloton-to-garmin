using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Helpers;
using Conversion;
using Dynastream.Fit;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Peloton;
using Serilog;
using System;
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
		//	var syncMyWorkoutFitFile = Path.Join(DataDirectory, "fenix_outdoor_run_vo2.fit");
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
		//	var file = Path.Join(DataDirectory, "file.json");
		//	var _fileHandler = new IOWrapper();
		//	var workout = _fileHandler.DeserializeJson<P2GWorkout>(file);
		//}

		//[Test]
		//public async Task Convert()
		//{
		//	var file = Path.Join(DataDirectory, "lanebreaker.json");

		//	var autoMocker = new AutoMocker();
		//	var settings = new Settings();

		//	var fitConverter = new ConverterInstance(settings);
		//	var messages = fitConverter.Convert(file);

		//	var output = Path.Join(DataDirectory, "output.fit");

		//	SaveFit(messages, output);
		//}

		private void SaveFit(Tuple<string, ICollection<Mesg>> messages, string outputPath)
		{
			using(FileStream fitDest = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
			{
				Encode encoder = new Encode(ProtocolVersion.V20);
				try
				{
					encoder.Open(fitDest);
					encoder.Write(messages.Item2);
				}
				finally
				{
					encoder.Close();
				}
			}
		}

		private void SaveRawData(dynamic data, string workoutId, string path)
		{
			System.IO.File.WriteAllText(Path.Join(path, $"{workoutId}_workout.json"), data.ToString());
		}

		private class ConverterInstance : FitConverter
		{
			private IOWrapper fileHandler = new IOWrapper();

			public ConverterInstance() : base(new Settings(), new IOWrapper()) { }
			public ConverterInstance(Settings settings) : base(settings, new IOWrapper()) { }

			public ICollection<Mesg> ConvertForTest(string path)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = this.Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData);

				return converted.Item2;
			}

			public Tuple<string, ICollection<Mesg>> Convert(string path)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = this.Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData);

				return converted;
			}
		}
	}
}
