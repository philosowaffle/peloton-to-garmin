using Common;
using Garmin;
using JsonFlatFileDataStore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Peloton;
using PelotonToFitConsole.Converter;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			Tracing.ValidateConfig(config.Observability);

			using (var metricsServer = EnableMetricsServer(config))
			using (var tracingServer = EnableTracing(config))
			using (Tracing.Source = new ActivitySource("P2G.Root"))
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
			var activity = Tracing.Source.StartActivity(nameof(RunAsync));

			var converted = new List<ConversionDetails>();
			var store = new DataStore(config.Application.SyncHistoryDbPath);

			IDocumentCollection<SyncHistoryItem> syncHistory = null;
			using (Metrics.DbActionDuration.WithLabels("using", "syncHistoryTable").NewTimer())
			using (var dbSapn = Tracing.Source.StartActivity("LoadTable").SetTag("table", "SyncHistoryItem"))
			{
				syncHistory = store.GetCollection<SyncHistoryItem>();
			}

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

			using var processWorkoutsSpan = Tracing.Source.StartActivity("ProcessingWorkouts");
			foreach (var recentWorkout in workoutsToConvert)
			{
				if (config.Observability.Prometheus.Enabled) Metrics.WorkoutsToConvert.Dec();
				using var processWorkoutSpan = Tracing.Source.StartActivity("ProcessingWorkout");

				SyncHistoryItem syncRecord = null;
				using (Metrics.DbActionDuration.WithLabels("select", "workoutId").NewTimer())
				using (Tracing.Source.StartActivity("LoadRecord").SetTag("table", "SyncHistoryItem"))
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
				using (Tracing.Source.StartActivity("Convert").SetTag("type", "fit"))
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
				using (Tracing.Source.StartActivity("UpsertRecord").SetTag("table", "SyncHistoryItem"))
					syncHistory.ReplaceOne(syncRecord.Id, syncRecord, upsert: true);
			}

			if (config.Garmin.Upload && converted.Any())
			{
				using (Metrics.WorkoutUploadDuration.WithLabels(converted.Count.ToString()).NewTimer())
				using (Tracing.Source.StartActivity("Upload").SetTag("target", "garmin"))
					GarminUploader.UploadToGarmin(converted.Select(c => c.Path).ToList(), config);
			}

			activity.Stop();
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

		private static TracerProvider EnableTracing(Configuration config)
		{
			TracerProvider tracing = null;
			if (!config.Observability.Jaeger.Enabled)
			{
				tracing = Sdk.CreateTracerProviderBuilder()
							.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("p2g"))
							.AddSource("P2G.Root", "P2G.Http", "P2G.Convert", "P2G.DB", "P2G.Upload")
							.AddJaegerExporter(o => 
							{
								o.AgentHost = config.Observability.Jaeger.AgentHost;
								o.AgentPort = config.Observability.Jaeger.AgentPort.GetValueOrDefault();
							})
							.Build();

				Console.Out.WriteLine($"Tracing started and exporting to: http://{config.Observability.Jaeger.AgentHost}:{config.Observability.Jaeger.AgentPort}");
			}

			return tracing;
		}
	}
}
