using Common;
using Common.Database;
using Common.Dto;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Metrics = Prometheus.Metrics;

namespace Conversion
{
	public interface IConverter
	{
		public void Convert();
		public void Decode(string filePath);
	}

	public abstract class Converter<T> : IConverter
	{
		private static readonly Gauge WorkoutsToConvert = Metrics.CreateGauge("p2g_workout_conversion_pending", "The number of workouts pending conversion to output format.",new GaugeConfiguration() 
		{
			LabelNames = new string[] { "type" }
		});
		private static readonly Counter WorkoutsConverted = Metrics.CreateCounter("p2g_workouts_converted", "The number of workouts converted.", new CounterConfiguration()
		{
			LabelNames = new string[] { "type" }
		});

		public static readonly float _metersPerMile = 1609.34f;

		protected Configuration _config;
		protected DbClient _dbClient;

		public Converter(Configuration config, DbClient dbClient)
		{
			_config = config;
			_dbClient = dbClient;
		}

		public abstract void Convert();
		public abstract void Decode(string filePath);

		protected abstract T Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary);

		protected abstract void Save(T data, string path);

		protected void Convert(string format)
		{
			if (!Directory.Exists(_config.App.DownloadDirectory))
			{
				Log.Information("No download directory found. Nothing to do. {@File}", _config.App.DownloadDirectory);
				return;
			}

			var files = Directory.GetFiles(_config.App.DownloadDirectory);

			if (files.Length == 0)
			{
				Log.Information("No files to convert in download directory. Nothing to do.");
				return;
			}

			if (_config.Garmin.Upload)
				FileHandling.MkDirIfNotEists(_config.App.UploadDirectory);

			// Foreach file in directory
			WorkoutsToConvert.WithLabels(format).Set(files.Count());
			using var timer = WorkoutsConverted.WithLabels(format).NewTimer();
			foreach (var file in files)
			{
				using var workoutTimer = WorkoutsConverted.WithLabels(format).NewTimer();

				// load file and deserialize
				P2GWorkout workoutData = null;
				try
				{
					using (var reader = new StreamReader(file))
					{
						workoutData = JsonSerializer.Deserialize<P2GWorkout>(reader.ReadToEnd(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
					}
				}
				catch (Exception e)
				{
					Log.Error(e, "Failed to load and parse workout data {@File}", file);
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
					WorkoutsToConvert.Dec();
					continue;
				}

				using var tracing = Tracing.Trace("Convert")
										?.WithWorkoutId(workoutData.Workout.Id)
										?.SetTag(TagKey.Format, format);

				// call internal convert method
				var converted = Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.WorkoutSummary);
				var workoutTitle = GetTitle(workoutData.Workout);

				if (converted is null)
				{
					Log.Error("Failed to convert workout data {@File}", file);
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
					WorkoutsToConvert.Dec();
					continue;
				}

				// write to output dir
				var path = Path.Join(_config.App.WorkingDirectory, $"{workoutTitle}.{format}");
				try
				{
					Save(converted, path);
				}
				catch (Exception e)
				{
					Log.Error(e, "Failed to write {@Format} file for {@Workout}", format, workoutTitle);
					WorkoutsToConvert.Dec();
					continue;
				}

				// copy to local save
				if (_config.Format.SaveLocalCopy)
				{
					try
					{
						FileHandling.MkDirIfNotEists(_config.App.TcxDirectory);
						FileHandling.MkDirIfNotEists(_config.App.FitDirectory);
						var dir = format == "fit" ? _config.App.FitDirectory : _config.App.TcxDirectory;

						var backupDest = Path.Join(dir, $"{workoutTitle}.{format}");
						System.IO.File.Copy(path, backupDest, overwrite: true);
						Log.Information("Backed up file {@File}", backupDest);
					}
					catch (Exception e)
					{
						Log.Error(e, "Failed to backup {@Format} file for {@Workout}", format, workoutTitle);
						continue;
					}
				}

				// copy to upload dir
				if (_config.Garmin.Upload && _config.Garmin.FormatToUpload == format)
				{
					try
					{
						var uploadDest = Path.Join(_config.App.UploadDirectory, $"{workoutTitle}.{format}");
						System.IO.File.Copy(path, uploadDest, overwrite: true);
						Log.Debug("Prepped {@Format} file {@Path} for upload.", format, uploadDest);
					}
					catch (Exception e)
					{
						Log.Error(e, "Failed to copy {@Format} file for {@Workout}", format, workoutTitle);
						continue;
					}
				}

				// update db item with conversion date
				SyncHistoryItem syncRecord = _dbClient.Get(workoutData.Workout.Id);
				if (syncRecord?.DownloadDate is null)
				{
					var startTimeInSeconds = workoutData.Workout.Start_Time;
					var dtDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToLocalTime();

					syncRecord = new SyncHistoryItem(workoutData.Workout)
					{
						DownloadDate = System.DateTime.Now
					};
				}

				syncRecord.ConvertedToFit = syncRecord.ConvertedToFit || format == "fit";
				syncRecord.ConvertedToTcx = syncRecord.ConvertedToTcx || format == "tcx";
				_dbClient.Upsert(syncRecord);
				WorkoutsToConvert.Dec();
				WorkoutsConverted.Inc();
			}
		}

		protected DateTime GetStartTime(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(startTimeInSeconds);
			return dtDateTime.ToUniversalTime();
		}

		protected string GetTimeStamp(DateTime startTime, long offset = 0)
		{
			return startTime.AddSeconds(offset).ToString("yyyy-MM-ddTHH:mm:ssZ");
		}

		protected float ConvertDistanceToMeters(double value, string unit)
		{
			switch (unit.ToLower())
			{
				case "km":
					return (float)value * 1000;
				case "mi":
					return (float)value * _metersPerMile;
				case "ft":
					return (float)value * 0.3048f;
				default:
					Log.Debug("Found unkown distance unit {@Unit}", unit);
					return (float)value;
			}
		}

		protected float GetTotalDistance(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
			{
				Log.Debug("No distance slug found. Defaulting to 0.");
				return 0.0f;
			}

			var unit = distanceSummary.Display_Unit;
			return ConvertDistanceToMeters(distanceSummary.Value, unit);
		}

		protected float ConvertToMetersPerSecond(double value, WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
			{
				Log.Debug("No distance slug found for unit. Defaulting distance to original value: {@Distance}", value);
				return (float)value;
			}

			var unit = distanceSummary.Display_Unit;
			var metersPerHour = ConvertDistanceToMeters(value, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;

			return metersPerSecond;
		}

		protected float GetMaxSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Metrics;
			var speedSummary = summaries.FirstOrDefault(s => s.Slug == "speed");
			if (speedSummary is null)
			{
				Log.Debug("No speed slug found. Defaulting to 0.");
				return 0.0f;
			}

			return ConvertToMetersPerSecond(speedSummary.Max_Value, workoutSamples);
		}

		protected float GetAvgSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Metrics;
			var speedSummary = summaries.FirstOrDefault(s => s.Slug == "speed");
			if (speedSummary is null)
			{
				Log.Debug("No speed slug found. Defaulting to 0.");
				return 0.0f;
			}

			return ConvertToMetersPerSecond(speedSummary.Average_Value, workoutSamples);
		}

		protected string GetTitle(Workout workout)
		{
			return $"{workout.Ride.Title} with {workout.Ride.Instructor.Name}"
				.Replace(" ", "_")
				.Replace("/", "-")
				.Replace(":", "-");
		}
	}
}
