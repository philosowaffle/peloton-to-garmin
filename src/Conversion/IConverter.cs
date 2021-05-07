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
using Summary = Common.Dto.Summary;

namespace Conversion
{
	public interface IConverter
	{
		public void Convert();
		public void Decode(string filePath);
	}

	public abstract class Converter<T> : IConverter
	{
		private static readonly Histogram WorkoutsConverted = Metrics.CreateHistogram("p2g_workouts_converted_duration_seconds", "The histogram of workouts converted.", new HistogramConfiguration()
		{
			LabelNames = new string[] { Common.Metrics.Label.FileType }
		});

		public static readonly float _metersPerMile = 1609.34f;

		protected Configuration _config;
		protected IDbClient _dbClient;

		public Converter(Configuration config, IDbClient dbClient)
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
					continue;
				}

				using var tracing = Tracing.Trace("Convert")
										.WithWorkoutId(workoutData.Workout.Id)
										.WithTag(TagKey.Format, format);

				// call internal convert method
				T converted = default;
				var workoutTitle = GetTitle(workoutData.Workout);
				try
				{
					converted = Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.WorkoutSummary);
					
				} catch (Exception e)
				{
					Log.Error(e, "Failed to convert workout data {@Workout} {@File}", workoutTitle, file);
				}				

				if (converted is null)
				{
					FileHandling.MoveFailedFile(file, _config.App.FailedDirectory);
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
			}
		}

		protected DateTime GetStartTimeUtc(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dateTime = DateTimeOffset.FromUnixTimeSeconds(startTimeInSeconds);
			return dateTime.UtcDateTime;
		}

		protected DateTime GetEndTimeUtc(Workout workout)
		{
			var endTimeSeconds = workout.End_Time;
			var dateTime = DateTimeOffset.FromUnixTimeSeconds(endTimeSeconds);
			return dateTime.UtcDateTime;
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
			var distanceSummary = GetDistanceSummary(workoutSamples);
			if (distanceSummary is null) return 0.0f;

			var unit = distanceSummary.Display_Unit;
			return ConvertDistanceToMeters(distanceSummary.Value, unit);
		}

		protected float ConvertToMetersPerSecond(double value, WorkoutSamples workoutSamples)
		{
			var distanceSummary = GetDistanceSummary(workoutSamples);
			if (distanceSummary is null) return (float)value;

			var unit = distanceSummary.Display_Unit;
			var metersPerHour = ConvertDistanceToMeters(value, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;

			return metersPerSecond;
		}

		private Summary GetDistanceSummary(WorkoutSamples workoutSamples)
		{
			if (workoutSamples?.Summaries is null)
			{
				Log.Debug("No workout Summaries found.");
				return null;
			}

			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
				Log.Debug("No distance slug found.");

			return distanceSummary;
		}

		protected float GetMaxSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var speedSummary = GetSpeedSummary(workoutSamples);
			if (speedSummary is null) return 0.0f;

			return ConvertToMetersPerSecond(speedSummary.Max_Value, workoutSamples);
		}

		protected float GetAvgSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var speedSummary = GetSpeedSummary(workoutSamples);
			if (speedSummary is null) return 0.0f;

			return ConvertToMetersPerSecond(speedSummary.Average_Value, workoutSamples);
		}

		private Metric GetSpeedSummary(WorkoutSamples workoutSamples)
		{
			if (workoutSamples?.Metrics is null)
			{
				Log.Debug("No workout Metrics found.");
				return null;
			}

			var speedSummary = workoutSamples.Metrics.FirstOrDefault(s => s.Slug == "speed");
			if (speedSummary is null)
				Log.Debug("No speed slug found.");

			return speedSummary;
		}

		protected string GetTitle(Workout workout)
		{
			var rideTitle = workout.Ride?.Title ?? workout.Id;
			var instructorName = workout.Ride?.Instructor?.Name;

			if (instructorName is object)
				instructorName = $" with {instructorName}";

			return $"{rideTitle}{instructorName}"
				.Replace(" ", "_")
				.Replace("/", "-")
				.Replace(":", "-");
		}

		protected byte? GetUserMaxHeartRate(WorkoutSamples workoutSamples) 
		{
			var maxZone = GetHeartRateZone(5, workoutSamples);

			return (byte)maxZone?.Max_Value;
		}

		protected Zone GetHeartRateZone(int zone, WorkoutSamples workoutSamples)
		{
			var hrData = workoutSamples.Metrics.FirstOrDefault(s => s.Slug == "heart_rate");

			if (hrData is null) return null;

			return hrData.Zones.FirstOrDefault(z => z.Slug == $"zone{zone}");
		}
	}
}
