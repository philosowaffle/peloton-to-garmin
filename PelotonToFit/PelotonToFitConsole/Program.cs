using Common;
using Common.Database;
using Garmin;
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
			using var activity = Tracing.Source.StartActivity(nameof(RunAsync))?
												.SetTag(Tracing.Category, Tracing.Default);

			var converted = new List<ConversionDetails>();
			var db = new DbClient(config);

			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			await pelotonApiClient.InitAuthAsync();
			var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(config.Peloton.NumWorkoutsToDownload);

			var workoutsToConvert = recentWorkouts.data.Where(w => w.Status == "COMPLETE");
			Metrics.WorkoutsToConvert.Set(workoutsToConvert.Count());
			using var processWorkoutsSpan = Tracing.Source.StartActivity("ProcessingWorkouts")?
															.SetTag(Tracing.Category, Tracing.Default);

			foreach (var recentWorkout in workoutsToConvert)
			{
				Metrics.WorkoutsToConvert.Dec();
				using var processWorkoutSpan = Tracing.Source.StartActivity("ProcessingWorkout")?
															.SetTag(Tracing.Category, Tracing.Default)?
															.SetTag(Tracing.WorkoutId, recentWorkout.Id);

				SyncHistoryItem syncRecord = db.Get(recentWorkout.Id);

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
			if (config.Observability.Jaeger.Enabled)
			{
				tracing = Sdk.CreateTracerProviderBuilder()
							.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("p2g"))
							.AddSource("P2G.Root")
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
