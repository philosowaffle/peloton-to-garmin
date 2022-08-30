using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Observe;
using Common.Stateful;
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
		ConvertStatus Convert(P2GWorkout workoutData);
	}

	public abstract class Converter<T> : IConverter
	{
		private static readonly Histogram WorkoutsConverted = Metrics.CreateHistogram($"{Statics.MetricPrefix}_workouts_converted_duration_seconds", "The histogram of workouts converted.", new HistogramConfiguration()
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

		public abstract ConvertStatus Convert(P2GWorkout workoutData);

		protected abstract T Convert(Workout workout, WorkoutSamples workoutSamples, UserData userData);

		protected abstract void Save(T data, string path);

		protected abstract void SaveLocalCopy(string sourcePath, string workoutTitle);

		protected ConvertStatus ConvertForFormat(FileFormat format, P2GWorkout workoutData)
		{
			using var tracing = Tracing.Trace($"{nameof(IConverter)}.{nameof(Convert)}.Workout")?
										.WithWorkoutId(workoutData.Workout.Id)
										.WithTag(TagKey.Format, format.ToString());

			var status = new ConvertStatus();

			// call internal convert method
			T converted = default;
			var workoutTitle = WorkoutHelper.GetUniqueTitle(workoutData.Workout);
			try
			{
				converted = Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to convert workout data to format {@Format} {@Workout}", format, workoutTitle);
				status.Success = false;
				status.ErrorMessage = "Failed to convert workout data.";
				tracing?.AddTag("excetpion.message", e.Message);
				tracing?.AddTag("exception.stacktrace", e.StackTrace);
				tracing?.AddTag("convert.success", false);
				tracing?.AddTag("convert.errormessage", status.ErrorMessage);
				return status;
			}

			// write to output dir
			var path = Path.Join(_config.App.WorkingDirectory, $"{workoutTitle}.{format}");
			try
			{
				_fileHandler.MkDirIfNotExists(_config.App.WorkingDirectory);
				Save(converted, path);
				status.Success = true;
			}
			catch (Exception e)
			{
				status.Success = false;
				status.ErrorMessage = "Failed to save converted workout for upload.";
				_logger.Error(e, "Failed to write {@Format} file for {@Workout}", format, workoutTitle);
				tracing?.AddTag("excetpion.message", e.Message);
				tracing?.AddTag("exception.stacktrace", e.StackTrace);
				tracing?.AddTag("convert.success", false);
				tracing?.AddTag("convert.errormessage", status.ErrorMessage);
				return status;
			}

			// copy to local save
			try
			{
				SaveLocalCopy(path, workoutTitle);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Failed to backup {@Format} file for {@Workout}", format, workoutTitle);
			}

			// copy to upload dir
			if (_config.Garmin.Upload && _config.Garmin.FormatToUpload == format)
			{
				try
				{
					var uploadDest = Path.Join(_config.App.UploadDirectory, $"{workoutTitle}.{format}");
					_fileHandler.MkDirIfNotExists(_config.App.UploadDirectory);
					_fileHandler.Copy(path, uploadDest, overwrite: true);
					_logger.Debug("Prepped {@Format} for upload: {@Path}", format, uploadDest);
				}
				catch (Exception e)
				{
					_logger.Error(e, "Failed to copy {@Format} file for {@Workout}", format, workoutTitle);
					status.Success = false;
					status.ErrorMessage = $"Failed to save file for {@format} and workout {workoutTitle} to Upload directory";
					tracing?.AddTag("excetpion.message", e.Message);
					tracing?.AddTag("exception.stacktrace", e.StackTrace);
					tracing?.AddTag("convert.success", false);
					tracing?.AddTag("convert.errormessage", status.ErrorMessage);
					return status;
				}
			}

			return status;
		}

		protected System.DateTime GetStartTimeUtc(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dateTime = DateTimeOffset.FromUnixTimeSeconds(startTimeInSeconds);
			return dateTime.UtcDateTime;
		}

		protected System.DateTime GetEndTimeUtc(Workout workout, WorkoutSamples workoutSamples)
		{
			var endTimeSeconds = workout.End_Time ?? workoutSamples.Duration + workout.Start_Time;
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
				case DistanceUnit.Meters:
				default:
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
				case "kph":
					return DistanceUnit.Kilometers;
				case "m":
					return DistanceUnit.Meters;
				case "mi":
				case "mph":
					return DistanceUnit.Miles;
				case "ft":
					return DistanceUnit.Feet;
				default:
					Log.Error("Found unknown distance unit {@Unit}", unit);
					return DistanceUnit.Unknown;
			}
		}

		protected ushort? GetCyclingFtp(Workout workout, UserData userData)
		{
			ushort? ftp = null;
			if (workout?.Ftp_Info is object && workout.Ftp_Info.Ftp > 0)
			{
				ftp = workout.Ftp_Info.Ftp;

				if (workout.Ftp_Info.Ftp_Source == CyclingFtpSource.Ftp_Manual_Source)
					ftp = (ushort)Math.Round(ftp.GetValueOrDefault() * .95);
			} 
			
			if ((ftp is null || ftp <= 0) && userData is object)
			{
				if (userData.Cycling_Ftp_Source == CyclingFtpSource.Ftp_Manual_Source)
					ftp = (ushort)Math.Round(userData.Cycling_Ftp * .95);

				if (userData.Cycling_Ftp_Source == CyclingFtpSource.Ftp_Workout_Source)
					ftp = userData.Cycling_Workout_Ftp;
			}

			if (ftp is null || ftp <= 0)
			{
				ftp = userData?.Estimated_Cycling_Ftp;
			}

			return ftp;
		}
	}
}
