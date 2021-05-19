using Common;
using Common.Dto;
using Conversion;
using FluentAssertions;
using Moq.AutoMock;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace UnitTests.Conversion
{
	public class ConverterTests
	{
		[Test]
		public void GetStartTimeTest()
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			var nowCst = DateTime.Now;
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
		public void GetTimeStampTest([Values(0,10)] long offset)
		{
			var now = DateTime.Parse("2021-04-01 03:14:12");

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
		public void ConvertDistanceToMetersTest([Values("km","mi","ft", "KM","unknown")]string unit)
		{
			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var value = 8677;
			var converted = converter.ConvertDistanceToMeters1(value, unit);
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

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var converted = converter.GetTotalDistance1(workoutSample);
			converted.Should().Be(0.0f);
		}

		[Test]
		public void GetTotalDistanceTest_NoDistanceSlug_Should_Return0()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = new List<Summary>() { new Summary() { Slug = "notDistance" } };

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var converted = converter.GetTotalDistance1(workoutSample);
			converted.Should().Be(0.0f);
		}

		[Test]
		public void GetTotalDistanceTest_Distance_Is_Converted_To_Meters([Values("mi", "ft", "km")] string unit)
		{
			var distance = 145;
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = new List<Summary>() 
			{ 
				new Summary() { Slug = "distance", Display_Unit = unit, Value = distance } 
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();
			var expectedDistance = converter.ConvertDistanceToMeters1(distance, unit);

			var converted = converter.GetTotalDistance1(workoutSample);
			converted.Should().Be(expectedDistance);
		}

		[Test]
		public void ConvertToMetersPerSecondTest_NullSummary_Should_Return_Original_Value()
		{
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = null;

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var value = 145;
			var converted = converter.ConvertToMetersPerSecond1(value, workoutSample);
			converted.Should().Be(value);
		}

		[Test]
		public void ConvertToMetersPerSecondTest_Is_Converted_To_MetersPerSecond([Values("mi", "ft", "km")] string unit)
		{
			var value = 145;
			var workoutSample = new WorkoutSamples();
			workoutSample.Summaries = new List<Summary>()
			{
				new Summary() { Slug = "distance", Display_Unit = unit, Value = value }
			};

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var metersPerHour = converter.ConvertDistanceToMeters1(value, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;
			var converted = converter.ConvertToMetersPerSecond1(value, workoutSample);
			converted.Should().Be(metersPerSecond);
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
		public void GetMaxSpeedMetersPerSecond_MaxSpeed_Is_Converted([Values("mi", "ft", "km")] string unit)
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
			var expectedDistance = converter.ConvertToMetersPerSecond1(speed, workoutSample);

			var converted = converter.GetMaxSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(expectedDistance);
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
		public void GetAvgSpeedMetersPerSecond_MaxSpeed_Is_Converted([Values("mi", "ft", "km")] string unit)
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
			var expectedDistance = converter.ConvertToMetersPerSecond1(speed, workoutSample);

			var converted = converter.GetAvgSpeedMetersPerSecond1(workoutSample);
			converted.Should().Be(expectedDistance);
		}

		private class ConverterInstance : Converter<string>
		{
			public ConverterInstance() : base(null, null, null) { }

			public override void Convert()
			{
				throw new NotImplementedException();
			}

			public override void Decode(string filePath)
			{
				throw new NotImplementedException();
			}

			protected override string Convert(Workout workout, WorkoutSamples workoutSamples, WorkoutSummary workoutSummary)
			{
				throw new NotImplementedException();
			}

			protected override void Save(string data, string path)
			{
				throw new NotImplementedException();
			}

			public DateTime GetStartTime1(Workout workout)
			{
				return this.GetStartTimeUtc(workout);
			}

			public string GetTimeStamp1(DateTime startTime, long offset)
			{
				return this.GetTimeStamp(startTime, offset);
			}

			public float ConvertDistanceToMeters1(double value, string unit)
			{
				return this.ConvertDistanceToMeters(value, unit);
			}

			public float GetTotalDistance1(WorkoutSamples workoutSamples)
			{
				return base.GetTotalDistance(workoutSamples);
			}

			public float ConvertToMetersPerSecond1(double value, WorkoutSamples workoutSamples)
			{
				return base.ConvertToMetersPerSecond(value, workoutSamples);
			}

			public float GetMaxSpeedMetersPerSecond1(WorkoutSamples workoutSamples)
			{
				return base.GetMaxSpeedMetersPerSecond(workoutSamples);
			}

			public float GetAvgSpeedMetersPerSecond1(WorkoutSamples workoutSamples)
			{
				return base.GetAvgSpeedMetersPerSecond(workoutSamples);
			}
		}
	}
}
