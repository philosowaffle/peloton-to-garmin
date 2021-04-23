using Common;
using Common.Database;
using Conversion;
using Garmin;
using Microsoft.Extensions.Configuration;
using Peloton;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Metrics = Prometheus.Metrics;

namespace PelotonToGarminConsole
{
	class Program
	{
		private static readonly Counter PollsCounter = Metrics.CreateCounter("p2g_polls_total", "The number of times the current process has polled for new data.");
		private static readonly Counter PollDuration = Metrics.CreateCounter("p2g_poll_duration_seconds", "Time of the entire poll run duration in seconds.");

		static void Main(string[] args)
		{
			Console.WriteLine("Peloton To Garmin");
			var config = new Configuration();

			try
			{
				IConfiguration configProviders = new ConfigurationBuilder()
				.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();
								
				configProviders.GetSection(nameof(App)).Bind(config.App);
				configProviders.GetSection(nameof(Format)).Bind(config.Format);
				configProviders.GetSection(nameof(Peloton)).Bind(config.Peloton);
				configProviders.GetSection(nameof(Garmin)).Bind(config.Garmin);
				configProviders.GetSection(nameof(Observability)).Bind(config.Observability);
				configProviders.GetSection(nameof(Developer)).Bind(config.Developer);

				// https://github.com/serilog/serilog-settings-configuration
				Log.Logger = new LoggerConfiguration()
					.ReadFrom.Configuration(configProviders, sectionName: $"{nameof(Observability)}:Serilog")
					.Enrich.WithSpan()
					.CreateLogger();
			} catch (Exception e)
			{
				Log.Error(e, "Exception during config setup.");
				throw;
			}			

			try
			{
				GarminUploader.ValidateConfig(config.Garmin);
				Common.Metrics.ValidateConfig(config.Observability);
				Tracing.ValidateConfig(config.Observability);

				if (config.Peloton.NumWorkoutsToDownload <= 0)
				{
					Console.Write("How many workouts to grab? ");
					int num = Convert.ToInt32(Console.ReadLine());
					config.Peloton.NumWorkoutsToDownload = num;
				}

				using var metrics = Common.Metrics.EnableMetricsServer(config.Observability.Prometheus);
				using var tracing = Tracing.EnableTracing(config.Observability.Jaeger);
				using var tracingSource = new ActivitySource("ROOT");

				FlurlConfiguration.Configure(config);

				if (config.App.EnablePolling)
				{
					while (true)
					{
						RunAsync(config).GetAwaiter().GetResult();
						Log.Information("Sleeping for {@Seconds} seconds...", config.App.PollingIntervalSeconds);
						Thread.Sleep(config.App.PollingIntervalSeconds * 1000);
					}
				}
				else
				{
					RunAsync(config).GetAwaiter().GetResult();
				}
			}
			catch (Exception e)
			{
				Log.Error(e, "Uncaught Exception.");
				throw;
			}
			finally
			{
				Log.CloseAndFlush();
				Console.ReadLine();
			}
		}

		static async Task RunAsync(Configuration config)
		{
			PollsCounter.Inc();
			using var timer = PollsCounter.NewTimer();
			using var activity = Tracing.Trace(nameof(RunAsync));

			var db = new DbClient(config);
			var pelotonApiClient = new Peloton.ApiClient(config.Peloton.Email, config.Peloton.Password);
			var peloton = new PelotonService(config, pelotonApiClient, db);

			await peloton.DownloadLatestWorkoutDataAsync();

			var fitConverter = new FitConverter(config, db);
			fitConverter.Convert();

			var tcxConverter = new TcxConverter(config, db);
			tcxConverter.Convert();

			var garminUploader = new GarminUploader(config);
			garminUploader.UploadToGarmin();

			FileHandling.Cleanup(config.App.DownloadDirectory);
			FileHandling.Cleanup(config.App.UploadDirectory);
			foreach (var file in Directory.GetFiles(config.App.WorkingDirectory))
				File.Delete(file);
		}
	}
}
