using Common;
using Common.Database;
using Garmin;
using Microsoft.Extensions.Configuration;
using Peloton;
using PelotonToFitConsole.Converter;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Metrics = Common.Metrics;

namespace PelotonToFitConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Peloton To FIT");

			IConfiguration configProviders = new ConfigurationBuilder()
				.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();

			var config = new Configuration();
			configProviders.GetSection(nameof(App)).Bind(config.App);
			configProviders.GetSection(nameof(Peloton)).Bind(config.Peloton);
			configProviders.GetSection(nameof(Garmin)).Bind(config.Garmin);
			configProviders.GetSection(nameof(Observability)).Bind(config.Observability);

			// TODO: Actually Verify Configuration validation
			GarminUploader.ValidateConfig(config.Garmin);
			Metrics.ValidateConfig(config.Observability);
			Tracing.ValidateConfig(config.Observability);

			using var metrics = Metrics.EnableMetricsServer(config.Observability.Prometheus);
			using var tracing = Tracing.EnableTracing(config.Observability.Jaeger);
			using var tracingSource = new ActivitySource("P2G.ROOT");

			FlurlConfiguration.Configure(config);

			if (config.App.EnablePolling)
			{
				while (true)
				{
					Metrics.PollsCounter.Inc();
					using(Metrics.PollDuration.NewTimer())
						RunAsync(config).GetAwaiter().GetResult();
					Console.Out.WriteLine($"Sleeping for {config.App.PollingIntervalSeconds} seconds...");
					Thread.Sleep(config.App.PollingIntervalSeconds * 1000);
				}
			}
			else
			{
				using (Metrics.PollDuration.NewTimer())
					RunAsync(config).GetAwaiter().GetResult();
			}
		}

		static async Task RunAsync(Configuration config)
		{
			using var activity = Tracing.Source?.StartActivity(nameof(RunAsync))?
												.SetTag(Tracing.Category, Tracing.Default);

			var converted = new List<ConversionDetails>();
			var db = new DbClient(config);

			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			await pelotonApiClient.InitAuthAsync();
			var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(config.Peloton.NumWorkoutsToDownload);

			var workoutsToConvert = recentWorkouts.data.Where(w => w.Status == "COMPLETE");
			Metrics.WorkoutsToConvert.Set(workoutsToConvert.Count());
			using var processWorkoutsSpan = Tracing.Source?.StartActivity("ProcessingWorkouts")?
															.SetTag(Tracing.Category, Tracing.Default);

			foreach (var recentWorkout in workoutsToConvert)
			{
				Metrics.WorkoutsToConvert.Dec();
				using var processWorkoutSpan = Tracing.Source?.StartActivity("ProcessingWorkout")?
															.SetTag(Tracing.Category, Tracing.Default)?
															.SetTag(Tracing.WorkoutId, recentWorkout.Id);

				SyncHistoryItem syncRecord = db.Get(recentWorkout.Id);

				if ((syncRecord?.ConvertedToFit ?? false) && config.Garmin.IgnoreSyncHistory == false)
				{
					if (config.Observability.LogLevel != Severity.None) Console.Out.Write($"Workout {recentWorkout.Id} already synced, skipping...");
					continue;
				}

				var workout = await pelotonApiClient.GetWorkoutByIdAsync(recentWorkout.Id);
				var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
				var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(recentWorkout.Id);

				var startTimeInSeconds = workout.Start_Time;
				var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
				dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToLocalTime();

				syncRecord = new SyncHistoryItem()
				{
					Id = workout.Id,
					WorkoutTitle = workout.Ride.Title,
					WorkoutDate = dtDateTime,
					DownloadDate = DateTime.Now
				};

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

				db.Upsert(syncRecord);
			}

			GarminUploader.UploadToGarmin(converted.Select(c => c.Path).ToList(), config);
		}
	}
}
