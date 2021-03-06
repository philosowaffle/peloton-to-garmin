using ActivityEncode;
using Dynastream.Fit;
using Peloton;
using PelotonToFitConsole.Converter;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PelotonToFitConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			if (!ConfigurationLoader.TryLoadConfigurationFile(out var config))
			{
				throw new ArgumentException("Failed to load configuration.");
			}

			// TODO: Configuration validation

			FlurlConfiguration.Configure(config);

			if (config.Application.EnablePolling)
			{
				while (true)
				{
					RunAsync(config).GetAwaiter().GetResult();
					Thread.Sleep(config.Application.PollingIntervalSeconds * 1000);
				}
			} else
			{
				RunAsync(config).GetAwaiter().GetResult();
			}
		}

		static async Task RunAsync(Configuration config)
		{
			Console.WriteLine("Hello World!");

			// TODO: Get workoutIds to convert
			// -- first check local DB for most recent convert
			// -- then query Peloton and look back until we find that id
			// -- grab all activities since then
			// -- logic to override via NUM instead??
			// -- need to handle when we purge the db and have no history, should it try to process all activities again?

			// TODO: enrich peloton data
			// -- once we have the list of workout ids, fetch the additional metadata we need from Peloton
			// -- make these requests async

			// TODO: Convert workouts
			// -- now, for each workout, convert to desired output
			// -- convert can probably be async process as well

			// TODO: Upload
			// -- if Garmin upload enabled then upload

			// TODO: Sleep till next wake up time
			

			//
			//
			//
			//


			//FitEncoderExample.CreateTimeBasedActivity();

			var fitConverter = new FitConverter();

			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			await pelotonApiClient.InitAuthAsync();

			//var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(70);
			//var wId = recentWorkouts.data.ElementAt(2);
			var wId = "1174a7ae422b4fb48ab6976d91341882"; // cadence and resistance
			var workout = await pelotonApiClient.GetWorkoutByIdAsync(wId);
			var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(wId);
			var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(wId);
			var response = fitConverter.Convert(workout, workoutSamples, workoutSummary, config);

			//foreach (var recentWorkout in recentWorkouts.data)
			//{
			//	if (recentWorkout.Status != "COMPLETE")
			//		continue;

			//	var workout = await pelotonApiClient.GetWorkoutByIdAsync(recentWorkout.Id);
			//	var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
			//	var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(recentWorkout.Id);
			//	var response = fitConverter.Convert(workout, workoutSamples, workoutSummary);
			//}

			//fitConverter.Decode("./20_min_Classic_Rock_Ride.fit");
			//fitConverter.Decode("G:\\5837503646_ACTIVITY.fit");
		}
	}
}
