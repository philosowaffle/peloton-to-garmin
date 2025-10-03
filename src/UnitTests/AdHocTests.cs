using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Http;
using Common.Service;
using Conversion;
using Dynastream.Fit;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Garmin;
using Garmin.Auth;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using Moq.AutoMock;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Peloton;
using Serilog;
using Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using PelotonApiClient = Peloton.ApiClient;

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

			// Allows using fiddler
			FlurlHttp.Clients.WithDefaults(cli =>
			{
				cli.ConfigureInnerHandler(handler =>
				{
					handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
				});
			});
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
		//public void DecodeFitFile()
		//{
		//	var output = Path.Join(FitDirectory, "strength_with_exercises.fit");
		//	FitDecoder.Decode(output);
		//}

		//[Test]
		//public async Task DownloadWorkout()
		//{
		//	var email = "";
		//	var password = "";

		//	var workoutId = "a077628f85954e979b2635490b6e3c86";
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

		//	var client = new PelotonApiClient(settingMock.Object);

		//	//var recentWorkouts = await client.GetWorkoutsAsync(userId, 5, 0);
		//	//var workouts = await client.GetWorkoutsAsync(System.DateTime.UtcNow.AddDays(-1), System.DateTime.UtcNow);
		//	var workout = await client.GetRawWorkoutByIdAsync(workoutId);
		//	var workoutSamples = await client.GetRawWorkoutSamplesByIdAsync(workoutId);
		//	//var workoutSegments = await client.GetRawClassSegmentsAsync("779fab8b986147f5af8bc385b4bc6bcf");
		//	//"tracking_type": "time_tracked_rep"

		//	//await client.GetUserDataAsync();

		//	//Log.Debug(workoutSamples.ToString());
		//	SaveRawData(workout, "blah", DataDirectory);
		//	//SaveRawData(workoutSegments, workoutId, DataDirectory);
		//	//SaveRawData(workoutSamples, workoutId, DataDirectory);
		//}

		//[Test]
		//public async Task DownloadAndSaveP2GWorkoutDetails()
		//{
		//	var email = "";
		//	var password = "";

		//	var workoutId = "a077628f85954e979b2635490b6e3c86";

		//	var settings = new Settings()
		//	{
		//		Peloton = new()
		//		{
		//			Email = email,
		//			Password = password,
		//		}
		//	};

		//	var autoMocker = new AutoMocker();
		//	var settingsService = autoMocker.GetMock<ISettingsService>();
		//	settingsService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
		//	settingsService.Setup(s => s.GetCustomDeviceInfoAsync(It.IsAny<Workout>())).ReturnsAsync(GarminDevices.EpixDevice);

		//	var fileHandler = autoMocker.GetMock<IFileHandling>();

		//	var client = new PelotonApiClient(settingsService.Object);
		//	var service = new PelotonService(settingsService.Object, client, fileHandler.Object);

		//	var p2gWorkout = await service.GetWorkoutDetailsAsync(workoutId);
		//	SaveData(p2gWorkout, workoutId, DataDirectory);

		//	// CONVERT TO FIT & SAVE
		//	var fitConverter = new ConverterInstance(settingsService.Object, new IOWrapper());
		//	var file = Path.Join(DataDirectory, $"{workoutId}_workout.json");
		//	var messages = await fitConverter.Convert(file, settings);

		//	var output = Path.Join(DataDirectory, "output.fit");

		//	fitConverter.Save(messages, output);
		//}

		//[Test]
		//public async Task DeSerialize()
		//{
		//	var file = Path.Join(DataDirectory, "workout.json");
		//	var _fileHandler = new IOWrapper();
		//	var workout = _fileHandler.DeserializeJson<RecentWorkouts>(file);
		//}

		//[Test]
		//public async Task Convert_From_File()
		//{
		//	var file = Path.Join(DataDirectory, "631fe107823048708d4c9f18a2888c6e_workout.json");
		//	//var file = Path.Join(DataDirectory, "cycling_target_metrics.json");
		//	//var file = Path.Join(DataDirectory, "tread_run_workout.json");

		//	var autoMocker = new AutoMocker();
		//	var settingsService = autoMocker.GetMock<SettingsService>();

		//	var settings = new Settings()
		//	{
		//	};
		//	var fileHandler = new IOWrapper();

		//	settingsService.SetReturnsDefault(settings);

		//	var fitConverter = new ConverterInstance(settingsService.Object, fileHandler);
		//	var messages = await fitConverter.Convert(file, settings);

		//	var output = Path.Join(DataDirectory, "output.fit");

		//	fitConverter.Save(messages, output);
		//}

		//[Test]
		//public async Task Convert_Stacked_Classes()
		//{
		//	var email = "";
		//	var password = "";

		//	var workoutId1 = "98c87f674a1848068d5aea14f73f3b1e"; // 98c87f674a1848068d5aea14f73f3b1e_10_min_Dalmatian_Coast_Ride.fit
		//	var workoutId2 = "3ce6e73be6434462a1940e20c1d61e5a"; // 3ce6e73be6434462a1940e20c1d61e5a_20_min_Bay_of_Kotor_Ride.fit

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
		//	settingMock.Setup(s => s.GetCustomDeviceInfoAsync(It.IsAny<Workout>())).ReturnsAsync(Format.DefaultDeviceInfoSettings[WorkoutType.Cycling]);

		//	var fileHandler = autoMocker.GetMock<IFileHandling>();

		//	// Download Workout data from Peloton
		//	//var client = new Peloton.ApiClient(settingMock.Object);
		//	//var service = new PelotonService(settingMock.Object, client, fileHandler.Object);

		//	//var p2gWorkout1 = await service.GetWorkoutDetailsAsync(workoutId1);
		//	//SaveData(p2gWorkout1, workoutId1, DataDirectory);
		//	//var p2gWorkout2 = await service.GetWorkoutDetailsAsync(workoutId2);
		//	//SaveData(p2gWorkout2, workoutId2, DataDirectory);

		//	// Load Workout data from File
		//	var p2gWorkout1Path = Path.Join(DataDirectory, $"{workoutId1}_workout.json");
		//	var p2gWorkout2Path = Path.Join(DataDirectory, $"{workoutId2}_workout.json");
		//	var _fileHandler = new IOWrapper();
		//	var p2gWorkout1 = _fileHandler.DeserializeJsonFile<P2GWorkout>(p2gWorkout1Path);
		//	var p2gWorkout2 = _fileHandler.DeserializeJsonFile<P2GWorkout>(p2gWorkout2Path);

		//	var stacks = StackedWorkoutsCalculator.GetStackedWorkouts(new List<P2GWorkout>() { p2gWorkout1,  p2gWorkout2 }, 300);
		//	var stackedClasses = StackedWorkoutsCalculator.CombineStackedWorkouts(stacks);

		//	var stackedWorkout = stackedClasses.FirstOrDefault();
		//	SaveData(stackedWorkout, "stackedWorkout", DataDirectory);

		//	var fitConverter = new ConverterInstance(settingMock.Object, fileHandler.Object);
		//	var messages = await fitConverter.Convert(stackedClasses.FirstOrDefault(), settings);

		//	var output = Path.Join(DataDirectory, "output.fit");

		//	fitConverter.Save(messages, output);
		//}

		private void SaveRawData(dynamic data, string workoutId, string path)
		{
			System.IO.File.WriteAllText(Path.Join(path, $"{workoutId}_workout.json"), data.ToString());
		}

		private void SaveData(object data, string fileName, string path)
		{
			var serializedData = JsonSerializer.Serialize(data, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, WriteIndented = true });
			System.IO.File.WriteAllText(Path.Join(path, $"{fileName}_workout.json"), serializedData.ToString());
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
				var workoutData = fileHandler.DeserializeJsonFile<P2GWorkout>(path);
				var converted = await this.ConvertInternalAsync(workoutData, settings);

				return converted.Item2;
			}

			public async Task<Tuple<string, ICollection<Mesg>>> Convert(string path, Settings settings)
			{
				var workoutData = fileHandler.DeserializeJsonFile<P2GWorkout>(path);
				var converted = await this.ConvertInternalAsync(workoutData, settings);

				return converted;
			}

			public async Task<Tuple<string, ICollection<Mesg>>> Convert(P2GWorkout workout, Settings settings)
			{
				var converted = await this.ConvertInternalAsync(workout, settings);

				return converted;
			}

			public new void Save(Tuple<string, ICollection<Mesg>> data, string path)
			{
				base.Save(data, path);
			}
		}
	}
}
