using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Http;
using Common.Service;
using Conversion;
using Dynastream.Fit;
using Flurl;
using Flurl.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Peloton;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitTests
{
	public class AdHocTests
	{
		private string DataDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data", "p2g_workouts");
		private string FitDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data", "sample_fit");

		[OneTimeSetUp]
		public void Setup()
		{
			Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.MinimumLevel.Verbose()
					//.MinimumLevel.Information()
					.CreateLogger();
		}

		//[Test]
		//public void EncryptionKeyGenerator()
		//{
		//	using var aesAlg = Aes.Create();

		//	aesAlg.GenerateKey();
		//	aesAlg.GenerateIV();

		//	var key = string.Join(", ", aesAlg.Key);
		//	var iv = string.Join(", ", aesAlg.IV);

		//	TestContext.Out.WriteLine("Key: " + key);
		//	TestContext.Out.WriteLine("IV: " + iv);
		//}

		//[Test]
		//public async Task AA()
		//{

		//}

		//[Test]
		//public void DecodeFitFile()
		//{
		//	var output = Path.Join(FitDirectory, "p2g_cycle_workout.fit");
		//	FitDecoder.Decode(output);
		//}

		//[Test]
		//public async Task DownloadWorkout()
		//{
		//	var email = "";
		//	var password = "";

		//	var workoutId = "";
		//	var userId = "";

		//	var settings = new Settings()
		//	{
		//		Peloton = new()
		//		{
		//			Email = email,
		//			Password = password,
		//		}
		//	};

		//	var autoMocker = new AutoMocker();
		//	var settingMock = autoMocker.GetMock<ISettingsService>();
		//	settingMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

		//	var client = new ApiClient(settingMock.Object);

		//	//var recentWorkouts = await client.GetWorkoutsAsync(userId, 5, 0);
		//	var workoutSamples = await client.GetWorkoutsAsync(System.DateTime.UtcNow.AddDays(-1), System.DateTime.UtcNow);
		//	await client.GetUserDataAsync();

		//	Log.Debug(workoutSamples.ToString());
		//	//SaveRawData(workoutSamples, workoutId, DataDirectory);
		//}

		//[Test]
		//public async Task DeSerialize()
		//{
		//	var file = Path.Join(DataDirectory, "workout.json");
		//	var _fileHandler = new IOWrapper();
		//	var workout = _fileHandler.DeserializeJson<RecentWorkouts>(file);
		//}

		//[Test]
		//public async Task Convert()
		//{
		//	var file = Path.Join(DataDirectory, "rower_workout.json");
		//	//var file = Path.Join(DataDirectory, "cycling_target_metrics.json");
		//	//var file = Path.Join(DataDirectory, "tread_run_workout.json");

		//	var autoMocker = new AutoMocker();
		//	var settingsService = autoMocker.GetMock<SettingsService>();

		//	var settings = new Settings()
		//	{
		//		Format = new Format()
		//		{
		//			Rowing = new Rowing() { PreferredLapType = PreferredLapType.Class_Segments }
		//		}
		//	};
		//	var fileHandler = new IOWrapper();

		//	settingsService.SetReturnsDefault(settings);

		//	var fitConverter = new ConverterInstance(settingsService.Object, fileHandler);
		//	var messages = await fitConverter.Convert(file, settings);

		//	var output = Path.Join(DataDirectory, "output.fit");

		//	fitConverter.Save(messages, output);
		//}

		private void SaveRawData(dynamic data, string workoutId, string path)
		{
			System.IO.File.WriteAllText(Path.Join(path, $"{workoutId}_workout.json"), data.ToString());
		}

		private async Task<JObject> GetRecentWorkoutsAsync(string userId, int numWorkouts = 3)
		{
			return await $"https://api.onepeloton.com/api/user/{userId}/workouts"
			.SetQueryParams(new
			{
				limit = numWorkouts,
				sort_by = "-created",
				page = 0,
				joins = "ride"
			})
			.GetJsonAsync<JObject>();
		}

		private class ConverterInstance : FitConverter
		{
			private IOWrapper fileHandler = new IOWrapper();

			public ConverterInstance(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

			public async Task<ICollection<Mesg>> ConvertForTest(string path, Settings settings)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = await this.ConvertInternalAsync(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData, settings);

				return converted.Item2;
			}

			public async Task<Tuple<string, ICollection<Mesg>>> Convert(string path, Settings settings)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = await this.ConvertInternalAsync(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData, settings);

				return converted;
			}

			public new void Save(Tuple<string, ICollection<Mesg>> data, string path)
			{
				base.Save(data, path);
			}
		}
	}
}
