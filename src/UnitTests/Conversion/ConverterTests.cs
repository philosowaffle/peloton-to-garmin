using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Service;
using Conversion;
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

		[Test]
		public async Task GetElevationGainAsync_When_ForceCalculation_True_Should_Calculate_EvenIfDisabled()
		{
			// SETUP
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 45, 50, 55 } },
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 12, 10, 8 } }
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = false,
				FlatRoadResistance = 30f,
				MaxGradePercentage = 15f
			};

			// ACT
			var result = await converter.GetElevationGain1(workout, workoutSamples, settings, forceCalculation: true);

			// ASSERT
			result.Should().NotBeNull();
			result.Should().BeGreaterThan(0f);
		}

		[Test]
		public async Task GetElevationGainAsync_When_ForceCalculation_False_And_Disabled_Should_ReturnNull()
		{
			// SETUP
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var workout = new Workout();
			var workoutSamples = new WorkoutSamples();
			var settings = new ElevationGainSettings { CalculateElevationGain = false };

			// ACT
			var result = await converter.GetElevationGain1(workout, workoutSamples, settings, forceCalculation: false);

			// ASSERT
			result.Should().BeNull();
		}

		[Test]
		public async Task GetElevationGainAsync_When_ForceCalculation_False_And_Enabled_Should_Calculate()
		{
			// SETUP
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 45, 50, 55 } },
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 12, 10, 8 } }
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f,
				MaxGradePercentage = 15f
			};

			// ACT
			var result = await converter.GetElevationGain1(workout, workoutSamples, settings, forceCalculation: false);

			// ASSERT
			result.Should().NotBeNull();
			result.Should().BeGreaterThan(0f);
		}

		// Static elevation gain calculation tests
		[Test]
		public async Task CalculateElevationGainAsync_WhenSettingsDisabled_ReturnsNull()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples();
			var settings = new ElevationGainSettings { CalculateElevationGain = false };

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().BeNull();
		}

		[Test]
		public async Task CalculateElevationGainAsync_WhenNoResistanceData_ReturnsNull()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>()
			};
			var settings = new ElevationGainSettings { CalculateElevationGain = true };

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().BeNull();
		}

		[Test]
		public async Task CalculateElevationGainAsync_WhenNoSpeedData_ReturnsNull()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 30, 35, 40 } }
				}
			};
			var settings = new ElevationGainSettings { CalculateElevationGain = true };

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().BeNull();
		}

		[Test]
		public async Task CalculateElevationGainAsync_WithFlatRoadResistance_ReturnsZero()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 30, 30, 30 } },
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 15, 15, 15 } }
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f
			};

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().Be(0f);
		}

		[Test]
		public async Task CalculateElevationGainAsync_WithClimbingResistance_CalculatesElevation()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 45, 50, 55 } }, // All above flat road (30)
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 12, 10, 8 } } // Decreasing speed
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f,
				MaxGradePercentage = 15f
			};

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeGreaterThan(0f);
			// Expected calculation:
			// Resistance 45: Grade = 15% * (15/70) = 3.21%, Speed = 12 mph = 5.36 m/s, Elevation = 5.36 * 3.21% = 0.172 m
			// Resistance 50: Grade = 15% * (20/70) = 4.29%, Speed = 10 mph = 4.47 m/s, Elevation = 4.47 * 4.29% = 0.192 m  
			// Resistance 55: Grade = 15% * (25/70) = 5.36%, Speed = 8 mph = 3.58 m/s, Elevation = 3.58 * 5.36% = 0.192 m
			// Total: 0.172 + 0.192 + 0.192 = 0.556 m
			result.Should().BeApproximately(0.556f, 0.01f);
		}

		[Test]
		public async Task CalculateElevationGainAsync_WithMixedResistance_OnlyCountsClimbing()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 25, 30, 35, 40 } }, // 25 below, 30 flat, 35&40 above
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 18, 15, 12, 10 } }
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f,
				MaxGradePercentage = 15f
			};

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeGreaterThan(0f);
			// Only resistance 35 and 40 should contribute to elevation gain
			// Resistance 25 and 30 should be ignored (below or equal to flat road)
		}

		[Test]
		public async Task CalculateElevationGainAsync_WithDifferentSpeedUnits_HandlesCorrectly()
		{
			// Arrange
			var workout = new Workout();
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 40, 45 } }, // Above flat road (30)
					new Metric { Slug = "speed", Display_Unit = "kph", Values = new double?[] { 20, 18 } } // km/h instead of mph
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f,
				MaxGradePercentage = 15f
			};

			// Act
			var result = await Converter<string>.CalculateElevationGainAsync(workout, workoutSamples, settings);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeGreaterThan(0f);
			// Should handle km/h conversion correctly
		}

		[Test]
		public void CalculateResistanceBasedElevationGain_WhenNoResistanceData_ReturnsNull()
		{
			// Arrange
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>()
			};
			var settings = new ElevationGainSettings { CalculateElevationGain = true };

			// Act
			var result = Converter<string>.CalculateResistanceBasedElevationGain(workoutSamples, settings);

			// Assert
			result.Should().BeNull();
		}

		[Test]
		public void CalculateResistanceBasedElevationGain_WhenNoSpeedData_ReturnsNull()
		{
			// Arrange
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 30, 35, 40 } }
				}
			};
			var settings = new ElevationGainSettings { CalculateElevationGain = true };

			// Act
			var result = Converter<string>.CalculateResistanceBasedElevationGain(workoutSamples, settings);

			// Assert
			result.Should().BeNull();
		}

		[Test]
		public void CalculateResistanceBasedElevationGain_WithFlatRoadResistance_ReturnsZero()
		{
			// Arrange
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 30, 30, 30 } },
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 15, 15, 15 } }
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f
			};

			// Act
			var result = Converter<string>.CalculateResistanceBasedElevationGain(workoutSamples, settings);

			// Assert
			result.Should().Be(0f);
		}

		[Test]
		public void CalculateResistanceBasedElevationGain_WithClimbingResistance_CalculatesElevation()
		{
			// Arrange
			var workoutSamples = new WorkoutSamples
			{
				Metrics = new List<Metric>
				{
					new Metric { Slug = "resistance", Values = new double?[] { 45, 50, 55 } }, // All above flat road (30)
					new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 12, 10, 8 } } // Decreasing speed
				}
			};
			var settings = new ElevationGainSettings 
			{ 
				CalculateElevationGain = true,
				FlatRoadResistance = 30f,
				MaxGradePercentage = 15f
			};

			// Act
			var result = Converter<string>.CalculateResistanceBasedElevationGain(workoutSamples, settings);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeGreaterThan(0f);
			// Expected calculation:
			// Resistance 45: Grade = 15% * (15/70) = 3.21%, Speed = 12 mph = 5.36 m/s, Elevation = 5.36 * 3.21% = 0.172 m
			// Resistance 50: Grade = 15% * (20/70) = 4.29%, Speed = 10 mph = 4.47 m/s, Elevation = 4.47 * 4.29% = 0.192 m  
			// Resistance 55: Grade = 15% * (25/70) = 5.36%, Speed = 8 mph = 3.58 m/s, Elevation = 3.58 * 5.36% = 0.192 m
			// Total: 0.172 + 0.192 + 0.192 = 0.556 m
			result.Should().BeApproximately(0.556f, 0.01f);
		}

		[Test]
		public void CalculateGradeFromResistance_WithFlatRoad_ReturnsZero()
		{
			// Arrange
			var resistance = 30f;
			var flatRoadResistance = 30f;
			var maxGrade = 15f;

			// Act
			var result = Converter<string>.CalculateGradeFromResistance(resistance, flatRoadResistance, maxGrade);

			// Assert
			result.Should().Be(0f);
		}

		[Test]
		public void CalculateGradeFromResistance_WithClimbing_CalculatesGrade()
		{
			// Arrange
			var resistance = 45f;
			var flatRoadResistance = 30f;
			var maxGrade = 15f;

			// Act
			var result = Converter<string>.CalculateGradeFromResistance(resistance, flatRoadResistance, maxGrade);

			// Assert
			// Expected: (45-30)/(100-30) * 15 = 15/70 * 15 = 3.21%
			result.Should().BeApproximately(3.21f, 0.01f);
		}

		[Test]
		public void CalculateGradeFromResistance_WithMaxResistance_ReturnsMaxGrade()
		{
			// Arrange
			var resistance = 100f;
			var flatRoadResistance = 30f;
			var maxGrade = 15f;

			// Act
			var result = Converter<string>.CalculateGradeFromResistance(resistance, flatRoadResistance, maxGrade);

			// Assert
			result.Should().Be(maxGrade);
		}

		[Test]
		public void CalculateGradeFromResistance_WithExcessiveResistance_CapsAtMaxGrade()
		{
			// Arrange
			var resistance = 120f; // Above max resistance
			var flatRoadResistance = 30f;
			var maxGrade = 15f;

			// Act
			var result = Converter<string>.CalculateGradeFromResistance(resistance, flatRoadResistance, maxGrade);

			// Assert
			result.Should().Be(maxGrade); // Should cap at max grade
		}

		private class ConverterInstance : Converter<string>
		{
			public ConverterInstance(ISettingsService settings, IFileHandling fileHandling) : base(settings, fileHandling) 
			{
				Format = FileFormat.Fit;
			}

			protected override Task<string> ConvertInternalAsync(P2GWorkout workout, Settings settings, bool forceElevationGainCalculation = false)
			{
				return Task.FromResult("");
			}

			protected override void Save(string data, string path)
			{
			}

			protected override bool ShouldConvert(Format settings) => true;

			public System.DateTime GetStartTime1(Workout workout)
			{
				return base.GetStartTimeUtc(workout);
			}

			public System.DateTime GetEndTimeUtc1(Workout workout, WorkoutSamples workoutSamples)
			{
				return base.GetEndTimeUtc(workout, workoutSamples);
			}

			public string GetTimeStamp1(System.DateTime startTime, long offset)
			{
				return base.GetTimeStamp(startTime, offset);
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

			public Task<GarminDeviceInfo> GetDeviceInfo1(Workout workout)
			{
				return base.GetDeviceInfoAsync(workout);
			}

			public ushort? GetCyclingFtp1(Workout workout, UserData userData)
			{
				return base.GetCyclingFtp(workout, userData);
			}

			public async Task<float?> GetElevationGain1(Workout workout, WorkoutSamples workoutSamples, ElevationGainSettings settings, bool forceCalculation)
			{
				return await GetElevationGainAsync(workout, workoutSamples, settings, forceCalculation);
			}
		}
	}
}
