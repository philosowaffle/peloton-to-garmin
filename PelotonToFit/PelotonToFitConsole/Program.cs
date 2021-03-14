using Common;
using Garmin;
using JsonFlatFileDataStore;
using Peloton;
using PelotonToFitConsole.Converter;
using Prometheus;
using System;
using System.Collections.Generic;
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

			if (!ConfigurationLoader.TryLoadConfigurationFile(out var config))
				throw new ArgumentException("Failed to load configuration.");

			// TODO: Actually Verify Configuration validation
			GarminUploader.ValidateConfig(config);
			Metrics.ValidateConfig(config.Observability);

			using (var metricsServer = EnableMetricsServer(config))
			{
				FlurlConfiguration.Configure(config);

				if (config.Application.EnablePolling)
				{
					while (true)
					{
						Metrics.PollsCounter.Inc();
						using(Metrics.PollDuration.NewTimer())
							RunAsync(config).GetAwaiter().GetResult();
						Console.Out.WriteLine($"Sleeping for {config.Application.PollingIntervalSeconds} seconds...");
						Thread.Sleep(config.Application.PollingIntervalSeconds * 1000);
					}
				}
				else
				{
					using (Metrics.PollDuration.NewTimer())
						RunAsync(config).GetAwaiter().GetResult();
				}
			}
		}

		static async Task RunAsync(Configuration config)
		{
			var converted = new List<ConversionDetails>();
			var store = new DataStore(config.Application.SyncHistoryDbPath);

			IDocumentCollection<SyncHistoryItem> syncHistory = null;
			using (Metrics.DbActionDuration.WithLabels("using", "syncHistoryTable").NewTimer())
				syncHistory = store.GetCollection<SyncHistoryItem>();

			// TODO: Get workoutIds to convert
			// -- first check local DB for most recent convert
			// -- then query Peloton and look back until we find that id
			// -- grab all activities since then
			// -- logic to override via NUM instead??
			// -- need to handle when we purge the db and have no history, should it try to process all activities again?
			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			await pelotonApiClient.InitAuthAsync();
			var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(config.Peloton.NumWorkoutsToDownload);

			var workoutsToConvert = recentWorkouts.data.Where(w => w.Status == "COMPLETE");
			if (config.Observability.Prometheus.Enabled)
				Metrics.WorkoutsToConvert.Set(workoutsToConvert.Count());

			foreach (var recentWorkout in workoutsToConvert)
			{
				if (config.Observability.Prometheus.Enabled) Metrics.WorkoutsToConvert.Dec();

				SyncHistoryItem syncRecord = null;
				using (Metrics.DbActionDuration.WithLabels("select", "workoutId").NewTimer())
					syncRecord = syncHistory.AsQueryable().Where(i => i.Id == recentWorkout.Id).FirstOrDefault();

				if ((syncRecord?.ConvertedToFit ?? false) && config.Garmin.IgnoreSyncHistory == false)
				{
					if (config.Application.DebugSeverity != Severity.None) Console.Out.Write($"Workout {recentWorkout.Id} already synced, skipping...");
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

				using (Metrics.WorkoutConversionDuration.WithLabels("fit").NewTimer())
				{
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
				}

				using (Metrics.DbActionDuration.WithLabels("upsert", "workoutId").NewTimer())
					syncHistory.ReplaceOne(syncRecord.Id, syncRecord, upsert: true);
			}

			if (config.Garmin.Upload && converted.Any())
			{
				using (Metrics.WorkoutUploadDuration.WithLabels(converted.Count.ToString()).NewTimer())
					GarminUploader.UploadToGarmin(converted.Select(c => c.Path).ToList(), config);
			}
		}

		private static IMetricServer EnableMetricsServer(Configuration config)
		{
			IMetricServer metricsServer = null;
			if (config.Observability.Prometheus.Enabled)
			{
				var port = config.Observability.Prometheus.Port ?? 4000;
				metricsServer = new KestrelMetricServer(port: port);
				metricsServer.Start();
				Console.Out.WriteLine($"Metrics Server started and listening on: http://localhost:{port}/metrics");
			}

			return metricsServer;
		}
	}
}
