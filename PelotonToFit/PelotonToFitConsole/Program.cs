using ActivityEncode;
using Peloton;
using PelotonToFitConsole.Converter;
using System;
using System.Threading.Tasks;

namespace PelotonToFitConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			Configuration.DebugSeverity = Severity.Debug;

			FlurlConfiguration.Configure();

			RunAsync().GetAwaiter().GetResult();
		}

		static async Task RunAsync()
		{
			Console.WriteLine("Hello World!");

			FitEncoderExample.CreateTimeBasedActivity();

			var fitConverter = new FitConverter();

			var pelotonApiClient = new ApiClient("@gmail.com", "");
			await pelotonApiClient.InitAuthAsync();

			var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(5);
			foreach (var recentWorkout in recentWorkouts.data)
			{
				if (recentWorkout.Status != "COMPLETE")
					continue;

				var workout = await pelotonApiClient.GetWorkoutByIdAsync(recentWorkout.Id);
				var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
				var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(recentWorkout.Id);
				var response = fitConverter.Convert(workout, workoutSamples, workoutSummary);
			}
		}
	}
}
