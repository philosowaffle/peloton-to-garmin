﻿using Common;
using Garmin;
using LiteDB;
using Peloton;
using PelotonToFitConsole.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PelotonToFitConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Peloton To FIT");

			if (!ConfigurationLoader.TryLoadConfigurationFile(out var config))
				throw new ArgumentException("Failed to load configuration.");

			// TODO: Configuration validation
			GarminUploader.ValidateConfig(config);

			FlurlConfiguration.Configure(config);

			if (config.Application.EnablePolling)
			{
				while (true)
				{
					RunAsync(config).GetAwaiter().GetResult();
					Console.Out.WriteLine($"Sleeping for {config.Application.PollingIntervalSeconds} seconds...");
					Thread.Sleep(config.Application.PollingIntervalSeconds * 1000);
				}
			} else
			{
				RunAsync(config).GetAwaiter().GetResult();
			}
		}

		static async Task RunAsync(Configuration config)
		{
			var converted = new List<ConversionDetails>();
			using (var syncDb = new LiteDatabase(config.Application.SyncHistoryDbPath))
			{
				var syncHistory = syncDb.GetCollection<SyncHistoryItem>("syncHistory");

				// TODO: Get workoutIds to convert
				// -- first check local DB for most recent convert
				// -- then query Peloton and look back until we find that id
				// -- grab all activities since then
				// -- logic to override via NUM instead??
				// -- need to handle when we purge the db and have no history, should it try to process all activities again?
				var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
				await pelotonApiClient.InitAuthAsync();
				var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(config.Peloton.NumWorkoutsToDownload);

				foreach (var recentWorkout in recentWorkouts.data.Where(w => w.Status == "COMPLETE"))
				{
					var syncRecord = syncHistory.Query().Where(i => i.WorkoutId == recentWorkout.Id).FirstOrDefault();

					if (config.Garmin.IgnoreSyncHistory == false || (syncRecord?.ConvertedToFit ?? false))
					{
						if (config.Application.DebugSeverity == Severity.Info) Console.Out.Write($"Workout {recentWorkout.Id} already synced, skipping..."); 
						continue;
					}

					var workout = await pelotonApiClient.GetWorkoutByIdAsync(recentWorkout.Id);
					var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
					var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(recentWorkout.Id);

					syncRecord = new SyncHistoryItem()
					{
						WorkoutId = workout.Id,
						WorkoutTitle = workout.Ride.Title,
						WorkoutDate = new DateTime(workout.Start_Time),
						DownloadDate = DateTime.Now
					};

					// TODO: Convert workouts
					// -- now, for each workout, convert to desired output
					// -- convert can probably be async process as well
					var fitConverter = new FitConverter();
					var convertedResponse = fitConverter.Convert(workout, workoutSamples, workoutSummary, config);
					syncRecord.ConvertedToFit = convertedResponse.Successful;
					if (convertedResponse.Successful)
					{
						converted.Add(convertedResponse);
					}
					else
					{
						Console.Out.WriteLine($"Failed to convert: {convertedResponse}");
					}

					syncHistory.Upsert(syncRecord);
				}
			}

			if (config.Garmin.Upload)
			{
				var uploadSuccess = GarminUploader.UploadToGarmin(converted.Select(c => c.Path).ToList(), config);
			}
		}
	}
}
