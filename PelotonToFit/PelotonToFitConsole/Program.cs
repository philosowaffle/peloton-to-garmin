﻿using Common;
using Garmin;
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
			{
				throw new ArgumentException("Failed to load configuration.");
			}

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
			var fitConverter = new FitConverter();

			// TODO: Get workoutIds to convert
			// -- first check local DB for most recent convert
			// -- then query Peloton and look back until we find that id
			// -- grab all activities since then
			// -- logic to override via NUM instead??
			// -- need to handle when we purge the db and have no history, should it try to process all activities again?
			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			await pelotonApiClient.InitAuthAsync();
			var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(config.Peloton.NumWorkoutsToDownload);
			//var recentWorkouts = new RecentWorkouts() { data = new List<Workout>() {  new Workout() { Id = "1174a7ae422b4fb48ab6976d91341882" } };
			 
			// TODO: enrich peloton data
			// -- once we have the list of workout ids, fetch the additional metadata we need from Peloton
			// -- optimize api calls, using joins query may not need to make three calls
			// -- make these requests async
			var converted = new List<ConversionDetails>();
			foreach (var workout  in recentWorkouts.data.Where(w => w.Status == "COMPLETE"))
			{
				var workoutEnriched = await pelotonApiClient.GetWorkoutByIdAsync(workout.Id);
				var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(workout.Id);
				var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(workout.Id);

				// TODO: Convert workouts
				// -- now, for each workout, convert to desired output
				// -- convert can probably be async process as well
				converted.Add(fitConverter.Convert(workoutEnriched, workoutSamples, workoutSummary, config));
			}

			var unsuccessfulConversions = converted.Where(c => c.Successful == false);
			if (unsuccessfulConversions.Any())
				foreach(var failure in unsuccessfulConversions)
				{
					Console.Out.WriteLine($"Failed to convert: {failure}");
				}

			if (config.Garmin.Upload)
			{
				GarminUploader.UploadToGarmin(converted.Where(p => p.Successful).Select(c => c.Path).ToList(), config);
			}
		}
	}
}