using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Conversion;
using Dynastream.Fit;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace UnitTests.Conversion
{
	public class FitConverterTests
	{
		private string DataDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data");
		private IOWrapper _fileHandler = new IOWrapper();

		[TestCase("cycling_workout", PreferredLapType.Default)]
		[TestCase("cycling_just_ride", PreferredLapType.Default)]
		[TestCase("tread_run_workout", PreferredLapType.Default)]
		[TestCase("meditation_workout", PreferredLapType.Default)]
		[TestCase("walking_workout_01", PreferredLapType.Default)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Default)]
		[TestCase("ride_based_on_distance", PreferredLapType.Default)]

		[TestCase("cycling_workout", PreferredLapType.Distance)]
		[TestCase("cycling_just_ride", PreferredLapType.Distance)]
		[TestCase("tread_run_workout", PreferredLapType.Distance)]
		[TestCase("meditation_workout", PreferredLapType.Distance)]
		[TestCase("walking_workout_01", PreferredLapType.Distance)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Distance)]
		[TestCase("ride_based_on_distance", PreferredLapType.Distance)]

		[TestCase("cycling_workout", PreferredLapType.Class_Segments)]
		[TestCase("cycling_just_ride", PreferredLapType.Class_Segments)]
		[TestCase("tread_run_workout", PreferredLapType.Class_Segments)]
		[TestCase("meditation_workout", PreferredLapType.Class_Segments)]
		[TestCase("walking_workout_01", PreferredLapType.Class_Segments)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Class_Segments)]
		[TestCase("ride_based_on_distance", PreferredLapType.Class_Segments)]

		[TestCase("cycling_workout", PreferredLapType.Class_Targets)]
		[TestCase("cycling_just_ride", PreferredLapType.Class_Targets)]
		[TestCase("tread_run_workout", PreferredLapType.Class_Targets)]
		[TestCase("meditation_workout", PreferredLapType.Class_Targets)]
		[TestCase("walking_workout_01", PreferredLapType.Class_Targets)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Class_Targets)]
		[TestCase("ride_based_on_distance", PreferredLapType.Class_Targets)]
		public void Fit_Converter_Creates_Valid_Fit(string filename, PreferredLapType lapType)
		{
			var workoutPath = Path.Join(DataDirectory, $"{filename}.json");
			var settings = new Settings()
			{
				Format = new Format()
				{
					Running = new Running()
					{
						PreferredLapType = lapType
					},
					Cycling = new Cycling()
					{
						PreferredLapType = lapType
					}
				}
			};
			var converter = new ConverterInstance(settings);
			var convertedMesgs = converter.ConvertForTest(workoutPath);

			convertedMesgs.Should().NotBeNullOrEmpty();

			var dest = Path.Join(DataDirectory, $"test_output_{filename}.fit");
			try
			{
				using (FileStream fitDest = new FileStream(dest, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
				{
					var validator = new Encode(ProtocolVersion.V20);
					validator.Open(fitDest);
					validator.Write(convertedMesgs); // validates while writing
					validator.Close();
				}
			} finally
			{
				System.IO.File.Delete(dest);
			}
		}

		[Test]
		public void Fit_Converter_Builds_Valid_DeviceInfoMesg([Values(5)] int major, [Values(0,-1,3,12)] int minor)
		{
			var info = new GarminDeviceInfo()
			{
				ManufacturerId = 1,
				Name = "test",
				ProductID = 2,
				UnitId = 3,
				Version = new GarminDeviceVersion()
				{
					VersionMajor = major,
					VersionMinor = minor,
				}
			};

			var converter = new ConverterInstance();

			var mesg = converter.GetDeviceInfo(info, new Dynastream.Fit.DateTime(System.DateTime.Now));

			mesg.GetSerialNumber().Should().Be(info.UnitId);
			mesg.GetManufacturer().Should().Be(info.ManufacturerId);
			mesg.GetProduct().Should().Be(info.ProductID);
			mesg.GetDeviceIndex().Should().Be(0);
			mesg.GetSourceType().Should().Be(SourceType.Local);

			var version = mesg.GetSoftwareVersion();

			if (minor <= 0)
			{
				version.Should().Be(5.0f);
			} else if (minor == 3)
			{
				version.Should().Be(5.3f);
			} else if (minor == 12)
			{
				version.Should().Be(5.12f);
			}
		}

		private class ConverterInstance : FitConverter
		{
			private IOWrapper fileHandler = new IOWrapper();

			public ConverterInstance() : base(new Settings(), null) { }

			public ConverterInstance(Settings settings) : base(settings, null) { }

			public ICollection<Mesg> ConvertForTest(string path)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = this.Convert(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData);

				return converted.Item2;
			}

			public DeviceInfoMesg GetDeviceInfo(GarminDeviceInfo deviceInfo, Dynastream.Fit.DateTime startTime)
			{
				return this.GetDeviceInfoMesg(deviceInfo, startTime);
			}
		}
	}
}
