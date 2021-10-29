using Common;
using Common.Dto;
using Common.Helpers;
using Conversion;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Peloton;
using Serilog;
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
		//	var autoMocker = new AutoMocker();
		//	var fitConverter = autoMocker.CreateInstance<FitConverter>();

		//	var syncMyWorkoutFitFile = Path.Join(DataDirectory, "garmin_training_effect.fit");
		//	fitConverter.Decode(syncMyWorkoutFitFile);
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

		private void SaveRawData(dynamic data, string workoutId, string path)
		{
			File.WriteAllText(Path.Join(path, $"{workoutId}_workout.json"), data.ToString());
		}
	}
}
