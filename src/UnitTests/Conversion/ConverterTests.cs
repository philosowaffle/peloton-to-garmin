using Common;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Service;
using Conversion;
using Dynastream.Fit;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.Conversion
{
	public class ConverterTests
	{
		[Test]
		public void GetStartTimeTest()
		{
			var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			var nowCst = System.DateTime.Now;
			TimeSpan diff = nowCst.ToUniversalTime() - epoch;
			var seconds = (long)Math.Floor(diff.TotalSeconds);

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var wokout = new Workout()
			{
				Start_Time = seconds
			};

			var startTime = converter.GetStartTime1(wokout);
			startTime.Should().BeCloseTo(nowCst.ToUniversalTime(), new TimeSpan(0,0,59));
		}

		[Test]
		public void GetEndTimeTest()
		{
			var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			var nowCst = System.DateTime.Now;
			TimeSpan diff = nowCst.ToUniversalTime() - epoch;
			var seconds = (long)Math.Floor(diff.TotalSeconds);

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var workoutSamples = new WorkoutSamples();
			var wokout = new Workout()
			{
				End_Time = seconds
			};

			var endTime = converter.GetEndTimeUtc1(wokout, workoutSamples);
			endTime.Should().BeCloseTo(nowCst.ToUniversalTime(), new TimeSpan(0, 0, 59));
		}

		[Test]
		public void GetEndTime_FallsbackToDuration_Test()
		{
			var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			var nowCst = System.DateTime.Now;
			TimeSpan diff = nowCst.ToUniversalTime() - epoch;
			var seconds = (long)Math.Floor(diff.TotalSeconds);

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var wokout = new Workout()
			{
				Start_Time = seconds,
				End_Time = null
			};

			var workoutSamples = new WorkoutSamples()
			{
				Duration = 500
			};

			var endTime = converter.GetEndTimeUtc1(wokout, workoutSamples);
			endTime.Should().BeCloseTo(nowCst.ToUniversalTime().AddSeconds(500), new TimeSpan(0, 0, 59));
		}

		[Test]
		public void GetTimeStampTest([Values(0,10)] long offset)
		{
			var now = System.DateTime.Parse("2021-04-01 03:14:12");

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var timeStamp = converter.GetTimeStamp1(now, offset);

			if (offset == 0)
			{
				timeStamp.Should().Be("2021-04-01T03:14:12Z");
			} else
			{
				timeStamp.Should().Be("2021-04-01T03:14:22Z");
			}
		}

		[Test]
		public void ConvertDistanceToMetersTest([Values("km","mi","ft", "KM", "m","unknown", "min/500m")]string unit)
		{
			var value = 8677;
			var converted = FitConverter.ConvertDistanceToMeters(value, unit);
			switch (unit.ToLower())
			{
				case "km":
					converted.Should().Be((float)value * 1000);
					break;
				case "mi":
					converted.Should().Be((float)value * ConverterInstance._metersPerMile);
					break;
				case "ft":
					converted.Should().Be((float)value * 0.3048f);
					break;
				case "min/500m":
					converted.Should().Be((float)value / 500);
					break;
				case "m":
				default:
					converted.Should().Be((float)value);
					break;
			}
		}

		[Test]
		public void GetTotalDistanceTest_NullSummary_Should_Return0()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = null;

			var converted = FitConverter.GetTotalDistance(workoutSample);
			converted.Should().Be(0.0f);
		}

		[Test]
		public void GetTotalDistanceTest_NoDistanceSlug_Should_Return0()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = new List<Summary>() { new Summary() { Slug = "notDistance" } };

			var converted = FitConverter.GetTotalDistance(workoutSample);
			converted.Should().Be(0.0f);
		}

		[Test]
		public void GetTotalDistanceTest_Distance_Is_Converted_To_Meters([Values("mi", "ft", "km", "m", "min/500m")] string unit)
		{
			var distance = 600;
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = new List<Summary>() 
			{ 
				new Summary() { Slug = "distance", Display_Unit = unit, Value = distance } 
			};

			var expectedDistance = FitConverter.ConvertDistanceToMeters(distance, unit);

			var converted = FitConverter.GetTotalDistance(workoutSample);
			converted.Should().Be(expectedDistance);
		}

		[Test]
		public void ConvertToMetersPerSecondTest_Is_Converted_To_MetersPerSecond([Values("mph", "kph")] string unit)
		{
			var value = 145;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var metersPerHour = FitConverter.ConvertDistanceToMeters(value, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;
			var converted = FitConverter.ConvertToMetersPerSecond(value, unit);
			converted.Should().Be(metersPerSecond);
		}

		[Test]
		public void ConvertToMetersPerSecondTest_Is_Converted_To_MetersPerSecond_ForRower([Values("min/500m")] string unit)
		{
			var value = 5;

			var converted = FitConverter.ConvertToMetersPerSecond(value, unit);
			converted.Should().Be(1.6666666F);
		}

		[Test]
		public void GetMaxSpeedMetersPerSecond_NullMetrics_Returns0()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var converted = converter.GetMaxSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(0.0f);
		}

		[Test]
		public void GetMaxSpeedMetersPerSecond_MaxSpeed_Is_Converted([Values("mi", "mph", "ft", "km", "kph", "m")] string unit)
		{
			var speed = 15.2;
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = new List<Metric>()
			{
				new Metric() { Slug = "speed", Display_Unit = unit, Max_Value = speed }
			};
			workoutSample.Summaries = new List<Summary>()
			{
				new Summary() { Slug = "distance", Display_Unit = unit }
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();
			var expectedDistance = FitConverter.ConvertToMetersPerSecond(speed, unit);

			var converted = converter.GetMaxSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(expectedDistance);
		}

		[Test]
		public void GetMaxSpeedMetersPerSecond_MaxSpeed_Is_Converted_ForRower([Values("min/500m")] string unit)
		{
			var speed = 5;
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = new List<Metric>()
			{
				new Metric() { Slug = "split_pace", Display_Unit = unit, Max_Value = speed }
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var converted = converter.GetMaxSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(1.6666666F);
		}

		[Test]
		public void GetAvgSpeedMetersPerSecond_NullMetrics_Returns0()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var converted = converter.GetAvgSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(0.0f);
		}

		[Test]
		public void GetAvgSpeedMetersPerSecond_MaxSpeed_Is_Converted([Values("mi", "mph", "ft", "km", "kph", "m")] string unit)
		{
			var speed = 15.2;
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = new List<Metric>()
			{
				new Metric() { Slug = "speed", Display_Unit = unit, Average_Value = speed }
			};
			workoutSample.Summaries = new List<Summary>()
			{
				new Summary() { Slug = "distance", Display_Unit = unit }
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();
			var expectedDistance = FitConverter.ConvertToMetersPerSecond(speed, unit);

			var converted = converter.GetAvgSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(expectedDistance);
		}

		[Test]
		public void GetAvgSpeedMetersPerSecond_MaxSpeed_Is_Converted_ForRower([Values("min/500m")] string unit)
		{
			var speed = 5;
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = new List<Metric>()
			{
				new Metric() { Slug = "split_pace", Display_Unit = unit, Average_Value = speed }
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var converted = converter.GetAvgSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(1.6666666F);
		}

		[Test]
		public void GetHeartRateZone_NullMetrics_ReturnsNull()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var zone = converter.GetHeartRateZone1(1, workoutSample);
			zone.Should().BeNull();
		}

		[Test]
		public void GetHeartRateZone_NullSamples_ReturnsNull()
		{
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var zone = converter.GetHeartRateZone1(1, null);
			zone.Should().BeNull();
		}

		[Test]
		public void GetHeartRateZone_NullZones_ReturnsNull()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = new List<Metric>()
			{
				new Metric()
				{
					Slug = "heart_rate",
					Zones = null
				}
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var zone = converter.GetHeartRateZone1(1, workoutSample);
			zone.Should().BeNull();
		}

		[Test]
		public void GetUserMaxHeartRate_NullMetrics_ReturnsNull()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Metrics = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var hr = converter.GetUserMaxHeartRate1(workoutSample);
			hr.Should().BeNull();
		}

		[Test]
		public void GetOutputSummary_NullWorkoutSamples_ReturnsNull()
		{
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var output = converter.GetOutputSummary1(null);
			output.Should().BeNull();
		}

		[Test]
		public void GetOutputSummary_Metrics_ReturnsNull()
		{
			var workoutSamples = new WorkoutSamples();
			workoutSamples.Metrics = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var output = converter.GetOutputSummary1(workoutSamples);
			output.Should().BeNull();
		}

		[Test]
		public void GetOutputSummary_NoCalorieSlug_ReturnsNull()
		{
			var workoutSamples = new WorkoutSamples();
			workoutSamples.Metrics = new List<Metric>() { new Metric() { Slug = "something" } };

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var output = converter.GetOutputSummary1(workoutSamples);
			output.Should().BeNull();
		}

		[Test]
		public void GetOutputSummary_CalorieSlug_ReturnsSummary()
		{
			var workoutSamples = new WorkoutSamples();
			workoutSamples.Metrics = new List<Metric>() { new Metric() { Slug = "output",  Max_Value = 100, Average_Value = 50 } };

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var output = converter.GetOutputSummary1(workoutSamples);
			output.Should().NotBeNull();
			output.Max_Value.Should().Be(100);
			output.Average_Value.Should().Be(50);
		}

		[Test]
		public void GetHeartRateSummary_NullWorkoutSamples_ReturnsNull()
		{
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var hr = converter.GetHeartRateSummary1(null);
			hr.Should().BeNull();
		}

		[Test]
		public void GetHeartRateSummary_Metrics_ReturnsNull()
		{
			var workoutSamples = new WorkoutSamples();
			workoutSamples.Metrics = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var hr = converter.GetHeartRateSummary1(workoutSamples);
			hr.Should().BeNull();
		}

		[Test]
		public void GetHeartRateSummary_NoCalorieSlug_ReturnsNull()
		{
			var workoutSamples = new WorkoutSamples();
			workoutSamples.Metrics = new List<Metric>() { new Metric() { Slug = "something" } };

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var hr = converter.GetHeartRateSummary1(workoutSamples);
			hr.Should().BeNull();
		}

		[Test]
		public void GetHeartRateSummary_CalorieSlug_ReturnsSummary()
		{
			var workoutSamples = new WorkoutSamples();
			workoutSamples.Metrics = new List<Metric>() { new Metric() { Slug = "heart_rate", Max_Value = 100, Average_Value = 50 } };

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var hr = converter.GetHeartRateSummary1(workoutSamples);
			hr.Should().NotBeNull();
			hr.Max_Value.Should().Be(100);
			hr.Average_Value.Should().Be(50);
		}

		[TestCase(FitnessDiscipline.Bike_Bootcamp)]
		[TestCase(FitnessDiscipline.Circuit)]
		[TestCase(FitnessDiscipline.Cardio)]
		[TestCase(FitnessDiscipline.Cycling)]
		[TestCase(FitnessDiscipline.Meditation)]
		[TestCase(FitnessDiscipline.Running)]
		[TestCase(FitnessDiscipline.Strength)]
		[TestCase(FitnessDiscipline.Stretching)]
		[TestCase(FitnessDiscipline.Walking)]
		[TestCase(FitnessDiscipline.Yoga)]
		public async Task GetDeviceInfo_ChoosesUserDevice_WhenProvided(FitnessDiscipline sport)
		{
			// SETUP
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();
			var settingsService = mocker.GetMock<ISettingsService>();

			GarminDeviceInfo outDevice = new GarminDeviceInfo()
			{
				Name = "UserDevice", // Max 20 Chars
				ProductID = GarminProduct.Amx,
				UnitId = 1,
				Version = new GarminDeviceVersion()
				{
					VersionMajor = 11,
					VersionMinor = 10,
					BuildMajor = 0,
					BuildMinor = 0,
				}
			};
			settingsService.Setup(s => s.GetCustomDeviceInfoAsync(It.IsAny<string>())).ReturnsAsync(outDevice);

			// ACT
			var deviceInfo = await converter.GetDeviceInfo1(sport, new Settings());

			// ASSERT
			deviceInfo.Name.Should().Be("UserDevice");
			deviceInfo.ProductID.Should().Be(GarminProduct.Amx);
			deviceInfo.UnitId.Should().Be(1);
			deviceInfo.Version.Should().NotBeNull();
			deviceInfo.Version.VersionMajor.Should().Be(11);
			deviceInfo.Version.VersionMinor.Should().Be(10);
			deviceInfo.Version.BuildMajor.Should().Be(0);
			deviceInfo.Version.BuildMinor.Should().Be(0);
		}

		[Test]
		public async Task GetDeviceInfo_FallsBackToDefault_WhenUserDeviceFailsToDeserialize()
		{
			// SETUP
			var mocker = new AutoMocker();
			var config = new Settings() { Format = new Format() { DeviceInfoPath = "somePath" } };
			mocker.Use(config);

			var converter = mocker.CreateInstance<ConverterInstance>();

			var fileHandler = mocker.GetMock<IFileHandling>();
			GarminDeviceInfo outDevice = null;
			fileHandler.Setup(x => x.TryDeserializeXml<GarminDeviceInfo>("somePath", out outDevice))
				.Callback(() =>
				{
					outDevice = new GarminDeviceInfo(); ;
				})
				.Returns(false);

			// ACT
			var deviceInfo = await converter.GetDeviceInfo1(FitnessDiscipline.Bike_Bootcamp, config);

			// ASSERT
			deviceInfo.Name.Should().Be("Forerunner 945");
			deviceInfo.ProductID.Should().Be(GarminProduct.Fr945);
			deviceInfo.UnitId.Should().Be(1);
			deviceInfo.Version.Should().NotBeNull();
			deviceInfo.Version.VersionMajor.Should().Be(19);
			deviceInfo.Version.VersionMinor.Should().Be(2);
			deviceInfo.Version.BuildMajor.Should().Be(0);
			deviceInfo.Version.BuildMinor.Should().Be(0);
		}

		[Test]
		public async Task GetDeviceInfo_ForCycling_ShouldReturn_CyclingDevice()
		{
			// SETUP
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();
			var config = new Settings();

			// ACT
			var deviceInfo = await converter.GetDeviceInfo1(FitnessDiscipline.Cycling, config);

			// ASSERT
			deviceInfo.Name.Should().Be("TacxTrainingAppWin");
			deviceInfo.ProductID.Should().Be(GarminProduct.TacxTrainingAppWin);
			deviceInfo.UnitId.Should().Be(1);
			deviceInfo.Version.Should().NotBeNull();
			deviceInfo.Version.VersionMajor.Should().Be(1);
			deviceInfo.Version.VersionMinor.Should().Be(30);
			deviceInfo.Version.BuildMajor.Should().Be(0);
			deviceInfo.Version.BuildMinor.Should().Be(0);
		}

		[TestCase(FitnessDiscipline.Bike_Bootcamp)]
		[TestCase(FitnessDiscipline.Circuit)]
		[TestCase(FitnessDiscipline.Cardio)]
		[TestCase(FitnessDiscipline.Meditation)]
		[TestCase(FitnessDiscipline.Running)]
		[TestCase(FitnessDiscipline.Strength)]
		[TestCase(FitnessDiscipline.Stretching)]
		[TestCase(FitnessDiscipline.Walking)]
		[TestCase(FitnessDiscipline.Yoga)]
		public async Task GetDeviceInfo_ForNonCycling_ShouldReturn_DefaultDevice(FitnessDiscipline sport)
		{
			// SETUP
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();
			var config = new Settings();

			// ACT
			var deviceInfo = await converter.GetDeviceInfo1(sport, config);

			// ASSERT
			deviceInfo.Name.Should().Be("Forerunner 945");
			deviceInfo.ProductID.Should().Be(GarminProduct.Fr945);
			deviceInfo.UnitId.Should().Be(1);
			deviceInfo.Version.Should().NotBeNull();
			deviceInfo.Version.VersionMajor.Should().Be(19);
			deviceInfo.Version.VersionMinor.Should().Be(2);
			deviceInfo.Version.BuildMajor.Should().Be(0);
			deviceInfo.Version.BuildMinor.Should().Be(0);
		}

		// Workout Object
		// manual source
		// workout source
		// UserData Obect
		// manual source
		// workout source
		class CyclingFtpScenarios
		{
			public static object[] Cases = 
			{
				new object[] { null, null, null },
				new object[] { new Workout(), new UserData(), (ushort)0 },
				new object[] { new Workout() {  Ftp_Info = new FTPInfo() { Ftp = 0} }, new UserData(), (ushort)0 },
				new object[] { new Workout() {  Ftp_Info = new FTPInfo() { Ftp = 100} }, new UserData(), (ushort)100 },
				new object[] { new Workout() {  Ftp_Info = new FTPInfo() { Ftp = 100, Ftp_Source = CyclingFtpSource.Ftp_Manual_Source} }, new UserData(), (ushort)95 },
				new object[] { null, new UserData() { Cycling_Ftp = 1, Cycling_Workout_Ftp = 2, Estimated_Cycling_Ftp = 100}, (ushort)100 },
				new object[] { null, new UserData() { Cycling_Ftp_Source = CyclingFtpSource.Ftp_Manual_Source, Cycling_Ftp = 100, Cycling_Workout_Ftp = 2, Estimated_Cycling_Ftp = 3}, (ushort)95 },
				new object[] { null, new UserData() { Cycling_Ftp_Source = CyclingFtpSource.Ftp_Workout_Source, Cycling_Ftp = 1, Cycling_Workout_Ftp = 100, Estimated_Cycling_Ftp = 3}, (ushort)100 },
				new object[] { null, new UserData() { Cycling_Ftp_Source = CyclingFtpSource.Ftp_Workout_Source, Cycling_Ftp = 1, Cycling_Workout_Ftp = 0, Estimated_Cycling_Ftp = 100}, (ushort)100 },
				new object[] { null, new UserData() { Cycling_Ftp_Source = CyclingFtpSource.Ftp_Manual_Source, Cycling_Ftp = 0, Cycling_Workout_Ftp = 0, Estimated_Cycling_Ftp = 100}, (ushort)100 },
			};
		}
		[TestCaseSource(typeof(CyclingFtpScenarios), nameof(CyclingFtpScenarios.Cases))]
		public void GetCyclingFtp_Should_PickCorrectValue(Workout workout, UserData userData, ushort? expectedFtp)
		{
			// SETUP
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();

			// ACT
			var ftp = converter.GetCyclingFtp1(workout, userData);

			// ASSERT
			ftp.Should().Be(expectedFtp);
		}

		// null & empty cases
		// Calorie Slug
		// TotalCalorie Slug
		class CalorieScenarios
		{
			public static object[] Cases =
			{
				new object[] { null, null },
				new object[] { new WorkoutSamples(), null },
				new object[] { new WorkoutSamples() { Summaries = new List<Summary>() }, null },
				new object[] { new WorkoutSamples() { Summaries = new List<Summary>() { new Summary() { Slug = "calories", Value = 10 } } }, 10 },
				new object[] { new WorkoutSamples() { Summaries = new List<Summary>() { new Summary() { Slug = "calories", Value = 10 }, new Summary() { Slug = "total_calories", Value = 20 } } }, 10 },
				new object[] { new WorkoutSamples() { Summaries = new List<Summary>() { new Summary() { Slug = "total_calories", Value = 20 } } }, 20 },
				new object[] { new WorkoutSamples() { Summaries = new List<Summary>() { new Summary() { Slug = "somethingElse", Value = 30 } } }, null },
			};
		}

		[TestCaseSource(typeof(CalorieScenarios), nameof(CalorieScenarios.Cases))]
		public void GetCalorieSummary_ShouldPickCorrectValue(WorkoutSamples samples, int? expectedCalories)
		{
			// SETUP
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();

			// ACT
			var calorieSummary = ConverterInstance.GetCalorieSummary(samples);

			// ASSERT
			calorieSummary?.Value.Should().Be(expectedCalories);
		}

		private class ConverterInstance : Converter<string>
		{
			public ConverterInstance(ISettingsService settings, IFileHandling fileHandling) : base(settings, fileHandling) 
			{
				Format = FileFormat.Fit;
			}

			protected override Task<string> ConvertInternalAsync(Workout workout, WorkoutSamples workoutSamples, UserData userData, Settings settings)
			{
				throw new NotImplementedException();
			}

			protected override void Save(string data, string path)
			{
				throw new NotImplementedException();
			}

			protected override bool ShouldConvert(Format settings) => true;

			public System.DateTime GetStartTime1(Workout workout)
			{
				return this.GetStartTimeUtc(workout);
			}

			public System.DateTime GetEndTimeUtc1(Workout workout, WorkoutSamples workoutSamples)
			{
				return this.GetEndTimeUtc(workout, workoutSamples);
			}

			public string GetTimeStamp1(System.DateTime startTime, long offset)
			{
				return this.GetTimeStamp(startTime, offset);
			}

			public float GetMaxSpeedMetersPerSecond1(WorkoutSamples workoutSamples)
			{
				return base.GetMaxSpeedMetersPerSecond(workoutSamples);
			}

			public float GetAvgSpeedMetersPerSecond1(WorkoutSamples workoutSamples)
			{
				return base.GetAvgSpeedMetersPerSecond(workoutSamples);
			}

			public Zone GetHeartRateZone1(int zone, WorkoutSamples workoutSamples)
			{
				return base.GetHeartRateZone(zone, workoutSamples);
			}

			public byte? GetUserMaxHeartRate1(WorkoutSamples workoutSamples)
			{
				return base.GetUserMaxHeartRate(workoutSamples);
			}

			public Metric GetOutputSummary1(WorkoutSamples workoutSamples)
			{
				return base.GetOutputSummary(workoutSamples);
			}

			public Metric GetHeartRateSummary1(WorkoutSamples workoutSamples)
			{
				return base.GetHeartRateSummary(workoutSamples);
			}
			public Task<GarminDeviceInfo> GetDeviceInfo1(FitnessDiscipline sport, Settings settings)
			{
				return base.GetDeviceInfoAsync(sport, settings);
			}

			public ushort? GetCyclingFtp1(Workout workout, UserData userData)
			{
				return base.GetCyclingFtp(workout, userData);
			}
		}
	}
}
