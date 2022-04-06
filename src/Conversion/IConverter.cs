using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Observe;
using Dynastream.Fit;
using Prometheus;
using Serilog;
using System;
using System.IO;
using System.Linq;
using Metrics = Prometheus.Metrics;
using Summary = Common.Dto.Peloton.Summary;

namespace Conversion
{
	public interface IConverter
	{
		public void Convert();
		public ConvertStatus Convert(P2GWorkout workoutData);
	}

	public abstract class Converter<T> : IConverter
	{
		private static readonly Histogram WorkoutsConverted = Metrics.CreateHistogram("p2g_workouts_converted_duration_seconds", "The histogram of workouts converted.", new HistogramConfiguration()
		{
			LabelNames = new string[] { Common.Observe.Metrics.Label.FileType }
		});

		private static readonly ILogger _logger = LogContext.ForClass<Converter<T>>();

		private static readonly GarminDeviceInfo CyclingDevice = new GarminDeviceInfo()
		{
			Name = "TacxTrainingAppWin", // Max 20 Chars
			ProductID = GarminProduct.TacxTrainingAppWin,
			UnitId = 1,
			ManufacturerId = 1, // Garmin
			Version = new GarminDeviceVersion()
			{
				VersionMajor = 1,
				VersionMinor = 30,
				BuildMajor = 0,
				BuildMinor = 0,
			}
		};

		private static readonly GarminDeviceInfo DefaultDevice = new GarminDeviceInfo()
		{
			Name = "Forerunner 945", // Max 20 Chars
			ProductID = GarminProduct.Fr945,
			UnitId = 1,
			ManufacturerId = 1, // Garmin
			Version = new GarminDeviceVersion()
			{
				VersionMajor = 19,
				VersionMinor = 2,
				BuildMajor = 0,
				BuildMinor = 0,
			}
		};

		public static readonly float _metersPerMile = 1609.34f;

		protected Settings _config;
		protected IFileHandling _fileHandler;

		public Converter(Settings config, IFileHandling fileHandler)
		{
			_config = config;
			_fileHandler = fileHandler;
		}

		public abstract void Convert();
		public abstract ConvertStatus Convert(P2GWorkout workoutData);

		protected abstract T Convert(Workout workout, WorkoutSamples workoutSamples);

		protected abstract void Save(T data, string path);

		protected ConvertStatus Convert(FileFormat format, P2GWorkout workoutData)
		{
			using var tracingConvert = Tracing.Trace($"{nameof(IConverter)}.{nameof(Convert)}.WithWorkoutData")
										.WithTag(TagKey.Format, format.ToString());
			
			var status = new ConvertStatus();

			if (_config.Garmin.Upload)
				_fileHandler.MkDirIfNotExists(_config.App.UploadDirectory);

			using var tracing = Tracing.Trace($"{nameof(IConverter)}.{nameof(Convert)}.Workout")
										.WithWorkoutId(workoutData.Workout.Id)
										.WithTag(TagKey.Format, format.ToString());

			// call internal convert method
			T converted = default;
			var workoutTitle = WorkoutHelper.GetUniqueTitle(workoutData.Workout);
			try
			{
				converted = Convert(workoutData.Workout, workoutData.WorkoutSamples);

			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to convert workout data to format {@Format} {@Workout}", format, workoutTitle);
				status.Success = false;
				status.ErrorMessage = "Failed to convert workout data.";
				return status;
			}

			// write to output dir
			var path = Path.Join(_config.App.WorkingDirectory, $"{workoutTitle}.{format}");
			try
			{
				Save(converted, path);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to write {@Format} file for {@Workout}", format, workoutTitle);
			}

			// copy to local save
			if (_config.Format.SaveLocalCopy)
			{
				try
				{
					_fileHandler.MkDirIfNotExists(_config.App.TcxDirectory);
					_fileHandler.MkDirIfNotExists(_config.App.FitDirectory);
					var dir = format == FileFormat.Fit ? _config.App.FitDirectory : _config.App.TcxDirectory;

					var backupDest = Path.Join(dir, $"{workoutTitle}.{format}");
					_fileHandler.Copy(path, backupDest, overwrite: true);
					_logger.Information("Backed up file {@File}", backupDest);
				}
				catch (Exception e)
				{
					_logger.Error(e, "Failed to backup {@Format} file for {@Workout}", format, workoutTitle);
				}
			}

			// copy to upload dir
			if (_config.Garmin.Upload && _config.Garmin.FormatToUpload == format)
			{
				try
				{
					var uploadDest = Path.Join(_config.App.UploadDirectory, $"{workoutTitle}.{format}");
					_fileHandler.Copy(path, uploadDest, overwrite: true);
					_logger.Debug("Prepped {@Format} for upload: {@Path}", format, uploadDest);
				}
				catch (Exception e)
				{
					_logger.Error(e, "Failed to copy {@Format} file for {@Workout}", format, workoutTitle);
					status.Success = false;
					status.ErrorMessage = $"Failed to save file for {@format} and workout {workoutTitle} to Upload directory";
					return status;
				}
			}

			return status;
		}

		protected void Convert(FileFormat format)
		{
			using var tracingConvert = Tracing.Trace($"{nameof(IConverter)}.{nameof(Convert)}")
										.WithTag(TagKey.Format, format.ToString());

			if (!_fileHandler.DirExists(_config.App.DownloadDirectory))
			{
				_logger.Information("[{@Format}] No download directory found. Nothing to do. {@File}", format, _config.App.DownloadDirectory);
				return;
			}

			var files = _fileHandler.GetFiles(_config.App.DownloadDirectory);

			if (files.Length == 0)
			{
				_logger.Information("[{@Format}] No files to convert in download directory. Nothing to do. {@File}", format, _config.App.DownloadDirectory);
				return;
			}

			_logger.Debug("[{@Format}] Files to convert: {@FileCount}", format, files.Length);

			if (_config.Garmin.Upload)
				_fileHandler.MkDirIfNotExists(_config.App.UploadDirectory);

			// Foreach file in directory
			foreach (var file in files)
			{
				using var workoutTimer = WorkoutsConverted.WithLabels(format.ToString()).NewTimer();

				// load file and deserialize
				P2GWorkout workoutData = null;
				try
				{
					workoutData = _fileHandler.DeserializeJson<P2GWorkout>(file);
				}
				catch (Exception e)
				{
					_logger.Error(e, "[{@Format}] Failed to load and parse workout data {@File}", format, file);
					_fileHandler.MoveFailedFile(file, _config.App.FailedDirectory);
					continue;
				}

				using var tracing = Tracing.Trace($"{nameof(IConverter)}.{nameof(Convert)}.Workout")
										.WithWorkoutId(workoutData.Workout.Id)
										.WithTag(TagKey.Format, format.ToString());

				// call internal convert method
				T converted = default;
				var workoutTitle = WorkoutHelper.GetUniqueTitle(workoutData.Workout);
				try
				{
					_logger.Debug("[{@Format}] Converting workout {@Workout}", format, workoutTitle);
					converted = Convert(workoutData.Workout, workoutData.WorkoutSamples);
					
				} catch (Exception e)
				{
					_logger.Error(e, "[{@Format}] Failed to convert workout data {@Workout} {@File}", format, workoutTitle, file);
				}

				if (converted is null)
				{
					_fileHandler.MoveFailedFile(file, _config.App.FailedDirectory);
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
					_logger.Error(e, "[{@Format}] Failed to write file for {@Workout}", format, workoutTitle);
					continue;
				}

				// copy to local save
				if (_config.Format.SaveLocalCopy)
				{
					try
					{
						_fileHandler.MkDirIfNotExists(_config.App.TcxDirectory);
						_fileHandler.MkDirIfNotExists(_config.App.FitDirectory);
						var dir = format == FileFormat.Fit ? _config.App.FitDirectory : _config.App.TcxDirectory;

						var backupDest = Path.Join(dir, $"{workoutTitle}.{format}");
						_fileHandler.Copy(path, backupDest, overwrite: true);
						_logger.Information("[{@Format}] Backed up file {@File}", format, backupDest);
					}
					catch (Exception e)
					{
						_logger.Error(e, "[{@Format}] Failed to backup file for {@Workout}", format, workoutTitle);
						continue;
					}
				}

				// copy to upload dir
				if (_config.Garmin.Upload && _config.Garmin.FormatToUpload == format)
				{
					try
					{
						var uploadDest = Path.Join(_config.App.UploadDirectory, $"{workoutTitle}.{format}");
						_fileHandler.Copy(path, uploadDest, overwrite: true);
						_logger.Debug("[{@Format}] File copied to upload directory: {@Path}", format, uploadDest);
					}
					catch (Exception e)
					{
						_logger.Error(e, "[{@Format}] Failed to copy file for {@Workout} to upload directory", format, workoutTitle);
						continue;
					}
				}
			}
		}

		protected System.DateTime GetStartTimeUtc(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dateTime = DateTimeOffset.FromUnixTimeSeconds(startTimeInSeconds);
			return dateTime.UtcDateTime;
		}

		protected System.DateTime GetEndTimeUtc(Workout workout)
		{
			var endTimeSeconds = workout.End_Time;
			var dateTime = DateTimeOffset.FromUnixTimeSeconds(endTimeSeconds);
			return dateTime.UtcDateTime;
		}

		protected string GetTimeStamp(System.DateTime startTime, long offset = 0)
		{
			return startTime.AddSeconds(offset).ToString("yyyy-MM-ddTHH:mm:ssZ");
		}

		protected float ConvertDistanceToMeters(double value, string unit)
		{
			var distanceUnit = GetDistanceUnit(unit);
			switch (distanceUnit)
			{
				case DistanceUnit.Kilometers:
					return (float)value * 1000;
				case DistanceUnit.Miles:
					return (float)value * _metersPerMile;
				case DistanceUnit.Feet:
					return (float)value * 0.3048f;
				default:
					Log.Verbose("Found unkown distance unit {@Unit}", unit);
					return (float)value;
			}
		}

		protected float GetTotalDistance(WorkoutSamples workoutSamples)
		{
			var distanceSummary = GetDistanceSummary(workoutSamples);
			if (distanceSummary is null) return 0.0f;

			var unit = distanceSummary.Display_Unit;
			return ConvertDistanceToMeters(distanceSummary.Value.GetValueOrDefault(), unit);
		}

		protected float ConvertToMetersPerSecond(double? value, WorkoutSamples workoutSamples)
		{
			var val = value.GetValueOrDefault();

			var distanceSummary = GetDistanceSummary(workoutSamples);
			if (distanceSummary is null) return (float)val;

			var unit = distanceSummary.Display_Unit;
			var metersPerHour = ConvertDistanceToMeters(val, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;

			return metersPerSecond;
		}

		private Summary GetDistanceSummary(WorkoutSamples workoutSamples)
		{
			if (workoutSamples?.Summaries is null)
			{
				_logger.Verbose("No workout Summaries found.");
				return null;
			}

			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
				_logger.Verbose("No distance slug found.");

			return distanceSummary;
		}

		protected Summary GetCalorieSummary(WorkoutSamples workoutSamples)
		{
			if (workoutSamples?.Summaries is null)
			{
				_logger.Verbose("No workout Summaries found.");
				return null;
			}

			var summaries = workoutSamples.Summaries;
			var caloriesSummary = summaries.FirstOrDefault(s => s.Slug == "calories");
			if (caloriesSummary is null)
				_logger.Verbose("No calories slug found.");

			return caloriesSummary;
		}

		protected float GetMaxSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var speedSummary = GetSpeedSummary(workoutSamples);
			if (speedSummary is null) return 0.0f;

			var max = speedSummary.Max_Value.GetValueOrDefault();
			return ConvertToMetersPerSecond(max, workoutSamples);
		}

		protected float GetAvgSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var speedSummary = GetSpeedSummary(workoutSamples);
			if (speedSummary is null) return 0.0f;

			var avg = speedSummary.Average_Value.GetValueOrDefault();
			return ConvertToMetersPerSecond(avg, workoutSamples);
		}

		protected float GetAvgGrade(WorkoutSamples workoutSamples)
		{
			var gradeSummary = GetGradeSummary(workoutSamples);
			if (gradeSummary is null) return 0.0f;

			return (float)gradeSummary.Average_Value;
		}
		protected float GetMaxGrade(WorkoutSamples workoutSamples)
		{
			var gradeSummary = GetGradeSummary(workoutSamples);
			if (gradeSummary is null) return 0.0f;

			return (float)gradeSummary.Max_Value;
		}

		protected Metric GetGradeSummary(WorkoutSamples workoutSamples)
		{
			return GetMetric("incline", workoutSamples);
		}

		protected Metric GetSpeedSummary(WorkoutSamples workoutSamples)
		{
			return GetMetric("speed", workoutSamples);
		}

		protected byte? GetUserMaxHeartRate(WorkoutSamples workoutSamples) 
		{
			var maxZone = GetHeartRateZone(5, workoutSamples);

			return (byte?)maxZone?.Max_Value;
		}

		protected Zone GetHeartRateZone(int zone, WorkoutSamples workoutSamples)
		{
			var hrData = GetHeartRateSummary(workoutSamples);

			if (hrData is null) return null;

			return hrData.Zones?.FirstOrDefault(z => z.Slug == $"zone{zone}");
		}

		protected PowerZones CalculatePowerZones(Workout workout)
		{
			var ftpMaybe = workout.Ftp_Info?.Ftp;
			if (ftpMaybe is null || ftpMaybe <= 0) return null;

			var ftp = ftpMaybe.Value;
			return new PowerZones()
			{
				Zone1 = new Zone()
				{
					Slug = "zone1",
					Min_Value = 0,
					Max_Value = 0.55 * ftp,
				},
				Zone2 = new Zone()
				{
					Slug = "zone2",
					Min_Value = 0.56 * ftp,
					Max_Value = 0.75 * ftp
				},
				Zone3 = new Zone()
				{
					Slug = "zone3",
					Min_Value = 0.76 * ftp,
					Max_Value = 0.90 * ftp
				},
				Zone4 = new Zone()
				{
					Slug = "zone4",
					Min_Value = 0.91 * ftp,
					Max_Value = 1.05 * ftp
				},
				Zone5 = new Zone()
				{
					Slug = "zone5",
					Min_Value = 1.06 * ftp,
					Max_Value = 1.20 * ftp
				},
				Zone6 = new Zone()
				{
					Slug = "zone6",
					Min_Value = 1.21 * ftp,
					Max_Value = 1.5 * ftp
				},
				Zone7 = new Zone()
				{
					Slug = "zone7",
					Min_Value = 1.51 * ftp,
					Max_Value = int.MaxValue
				}
			};
		}

		protected PowerZones GetTimeInPowerZones(Workout workout, WorkoutSamples workoutSamples)
		{
			var powerZoneData = GetOutputSummary(workoutSamples);

			if (powerZoneData is null) return null;

			var zones = CalculatePowerZones(workout);

			if (zones is null) return null;

			foreach (var value in powerZoneData.Values)
			{
				if (zones.Zone1.Min_Value <= value
					&& zones.Zone1.Max_Value >= value)
				{
					zones.Zone1.Duration++;
					continue;
				}

				if (zones.Zone2.Min_Value <= value
					&& zones.Zone2.Max_Value >= value)
				{
					zones.Zone2.Duration++;
					continue;
				}

				if (zones.Zone3.Min_Value <= value
					&& zones.Zone3.Max_Value >= value)
				{
					zones.Zone3.Duration++;
					continue;
				}

				if (zones.Zone4.Min_Value <= value
					&& zones.Zone4.Max_Value >= value)
				{
					zones.Zone4.Duration++;
					continue;
				}

				if (zones.Zone5.Min_Value <= value
					&& zones.Zone5.Max_Value >= value)
				{
					zones.Zone5.Duration++;
					continue;
				}

				if (zones.Zone6.Min_Value <= value
					&& zones.Zone6.Max_Value >= value)
				{
					zones.Zone6.Duration++;
					continue;
				}

				if (zones.Zone7.Min_Value <= value
					&& zones.Zone7.Max_Value >= value)
				{
					zones.Zone7.Duration++;
					continue;
				}
			}

			return zones;
		}

		protected Metric GetOutputSummary(WorkoutSamples workoutSamples)
		{
			return GetMetric("output", workoutSamples);
		}

		protected Metric GetHeartRateSummary(WorkoutSamples workoutSamples)
		{
			return GetMetric("heart_rate", workoutSamples);
		}

		protected Metric GetCadenceSummary(WorkoutSamples workoutSamples)
		{
			return GetMetric("cadence", workoutSamples);
		}

		protected Metric GetResistanceSummary(WorkoutSamples workoutSamples)
		{
			return GetMetric("resistance", workoutSamples);
		}

		protected Metric GetMetric(string slug, WorkoutSamples workoutSamples)
		{
			if (workoutSamples?.Metrics is null)
			{
				_logger.Verbose("No workout Metrics found.");
				return null;
			}

			var metric = workoutSamples.Metrics.FirstOrDefault(s => s.Slug == slug);
			if (metric is null)
			{
				var alts = workoutSamples.Metrics
					.Where(w => w.Alternatives is object)
					.SelectMany(s => s.Alternatives);
				metric = alts.FirstOrDefault(s => s.Slug == slug);
			}

			if (metric is null)
				_logger.Verbose($"No {slug} found.");

			return metric;
		}

		protected GarminDeviceInfo GetDeviceInfo(FitnessDiscipline sport)
		{
			GarminDeviceInfo userProvidedDeviceInfo = null;
			var userDevicePath = _config.Format.DeviceInfoPath;

			if (!string.IsNullOrEmpty(userDevicePath))
			{
				if(_fileHandler.TryDeserializeXml(userDevicePath, out userProvidedDeviceInfo))
					return userProvidedDeviceInfo;
			}

			if(sport == FitnessDiscipline.Cycling)
				return CyclingDevice;

			return DefaultDevice;
		}

		protected DistanceUnit GetDistanceUnit(string unit)
		{
			switch (unit?.ToLower())
			{
				case "km":
					return DistanceUnit.Kilometers;
				case "mi":
					return DistanceUnit.Miles;
				case "ft":
					return DistanceUnit.Feet;
				default:
					Log.Verbose("Found unkown distance unit {@Unit}", unit);
					return DistanceUnit.Unknown;
			}
		}
	}
}
