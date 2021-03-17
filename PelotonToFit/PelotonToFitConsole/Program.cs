using Common;
using Common.Database;
using Garmin;
using Microsoft.Extensions.Configuration;
using Peloton;
using PelotonToFitConsole.Converter;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
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

			// TODO: document how to configure this and which sinks are supported
			// https://github.com/serilog/serilog-settings-configuration
			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(configProviders, sectionName: $"{nameof(Observability)}:Serilog")
				.Enrich.WithSpan()
				.CreateLogger();

			try
			{
				// TODO: Actually Verify Configuration validation
				GarminUploader.ValidateConfig(config.Garmin);
				Metrics.ValidateConfig(config.Observability);
				Tracing.ValidateConfig(config.Observability);

				using var metrics = Metrics.EnableMetricsServer(config.Observability.Prometheus);
				using var tracing = Tracing.EnableTracing(config.Observability.Jaeger);
				using var tracingSource = new ActivitySource("ROOT");

				FlurlConfiguration.Configure(config);

				if (config.App.EnablePolling)
				{
					while (true)
					{
						Metrics.PollsCounter.Inc();
						using (Metrics.PollDuration.NewTimer())
							RunAsync(config).GetAwaiter().GetResult();
						Log.Information("Sleeping for {0} seconds...", config.App.PollingIntervalSeconds);
						Thread.Sleep(config.App.PollingIntervalSeconds * 1000);
					}
				}
				else
				{
					using (Metrics.PollDuration.NewTimer())
						RunAsync(config).GetAwaiter().GetResult();
				}
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		static async Task RunAsync(Configuration config)
		{
			using var activity = Tracing.Trace(nameof(RunAsync));
			Log.Information("test");

			var converted = new List<ConversionDetails>();
			var db = new DbClient(config);

			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			await pelotonApiClient.InitAuthAsync();
			var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(config.Peloton.NumWorkoutsToDownload);

			var workoutsToConvert = recentWorkouts.data.Where(w => w.Status == "COMPLETE");
			Metrics.WorkoutsToConvert.Set(workoutsToConvert.Count());
			using var processWorkoutsSpan = Tracing.Trace("ProcessingWorkouts");

			foreach (var recentWorkout in workoutsToConvert)
			{
				Metrics.WorkoutsToConvert.Dec();
				using var processWorkoutSpan = Tracing.Trace("ProcessingWorkout").WithWorkoutId(recentWorkout.Id);

				SyncHistoryItem syncRecord = db.Get(recentWorkout.Id);

				if ((syncRecord?.ConvertedToFit ?? false) && config.Garmin.IgnoreSyncHistory == false)
				{
					Log.Information("Workout {0} already synced, skipping.", recentWorkout.Id);
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
					Log.Error("Failed to convert: {0}", convertedResponse);
				}

				db.Upsert(syncRecord);
			}

			GarminUploader.UploadToGarmin(converted.Select(c => c.Path).ToList(), config);
		}
	}
}
