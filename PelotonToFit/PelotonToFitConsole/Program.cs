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

			var converted = new List<ConversionDetails>();
			var db = new DbClient(config);
			var pelotonApiClient = new ApiClient(config.Peloton.Email, config.Peloton.Password);
			var peloton = new PelotonData(config, pelotonApiClient, db);

			var workoutDatas = await peloton.DownloadLatestWorkoutDataAsync();

			Metrics.WorkoutsToConvert.Set(workoutDatas.Count());
			
			using var processWorkoutsSpan = Tracing.Trace("ProcessingWorkouts");

			foreach (var workoutData in workoutDatas)
			{
				Metrics.WorkoutsToConvert.Dec();
				using var processWorkoutSpan = Tracing.Trace("ProcessingWorkout").WithWorkoutId(workoutData.Workout.Id);

				var fitConverter = new FitConverter();
				var convertedResponse = fitConverter.Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.WorkoutSummary, config);
				workoutData.SyncHistoryItem.ConvertedToFit = convertedResponse.Successful;
				if (convertedResponse.Successful)
				{
					converted.Add(convertedResponse);
				}
				else
				{
					Log.Error("Failed to convert: {0}", convertedResponse);
				}

				db.Upsert(workoutData.SyncHistoryItem);
			}

			GarminUploader.UploadToGarmin(converted.Select(c => c.Path).ToList(), config);
		}
	}
}
