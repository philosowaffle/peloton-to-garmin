using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Service;
using Conversion;
using Dynastream.Fit;
using FluentAssertions;
using Moq.AutoMock;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UnitTests.Conversion
{
	public class FitConverterTests
	{
		private string DataDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data", "p2g_workouts");

		[Test]
		public void Converter_Should_Provide_Formt_of_FIT()
		{
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();

			converter.Format.Should().Be(FileFormat.Fit);
		}

		[Test]
		public void ShouldConvert_ShouldOnly_Support_Fit([Values] bool tcx, [Values] bool json, [Values] bool fit)
		{
			var mocker = new AutoMocker();
			var converter = mocker.CreateInstance<ConverterInstance>();

			var formatSettings = new Format()
			{
				Fit = fit,
				Tcx = tcx,
				Json = json
			};

			converter.ShouldConvert(formatSettings).Should().Be(fit);
		}

		[TestCase("cycling_workout", PreferredLapType.Default)]
		[TestCase("cycling_just_ride", PreferredLapType.Default)]
		[TestCase("tread_run_workout", PreferredLapType.Default)]
		[TestCase("meditation_workout", PreferredLapType.Default)]
		[TestCase("walking_outdoor_01", PreferredLapType.Default)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Default)]
		[TestCase("ride_based_on_distance", PreferredLapType.Default)]
		[TestCase("rower_workout", PreferredLapType.Default)]

		[TestCase("cycling_workout", PreferredLapType.Distance)]
		[TestCase("cycling_just_ride", PreferredLapType.Distance)]
		[TestCase("tread_run_workout", PreferredLapType.Distance)]
		[TestCase("meditation_workout", PreferredLapType.Distance)]
		[TestCase("walking_outdoor_01", PreferredLapType.Distance)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Distance)]
		[TestCase("ride_based_on_distance", PreferredLapType.Distance)]
		[TestCase("rower_workout", PreferredLapType.Distance)]

		[TestCase("cycling_workout", PreferredLapType.Class_Segments)]
		[TestCase("cycling_just_ride", PreferredLapType.Class_Segments)]
		[TestCase("tread_run_workout", PreferredLapType.Class_Segments)]
		[TestCase("meditation_workout", PreferredLapType.Class_Segments)]
		[TestCase("walking_outdoor_01", PreferredLapType.Class_Segments)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Class_Segments)]
		[TestCase("ride_based_on_distance", PreferredLapType.Class_Segments)]
		[TestCase("rower_workout", PreferredLapType.Class_Segments)]

		[TestCase("cycling_workout", PreferredLapType.Class_Targets)]
		[TestCase("cycling_just_ride", PreferredLapType.Class_Targets)]
		[TestCase("tread_run_workout", PreferredLapType.Class_Targets)]
		[TestCase("meditation_workout", PreferredLapType.Class_Targets)]
		[TestCase("walking_outdoor_01", PreferredLapType.Class_Targets)]
		[TestCase("running_workout_no_metrics", PreferredLapType.Class_Targets)]
		[TestCase("ride_based_on_distance", PreferredLapType.Class_Targets)]
		[TestCase("rower_workout", PreferredLapType.Class_Targets)]
		public async Task Fit_Converter_Creates_Valid_Fit(string filename, PreferredLapType lapType)
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

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

			var convertedMesgs = await converter.ConvertForTest(workoutPath, settings);

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

			var autoMocker = new AutoMocker();
			var converter = autoMocker.CreateInstance<ConverterInstance>();

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

			public new FileFormat Format => base.Format;

			public ConverterInstance(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

			public ConverterInstance(ISettingsService settings) : base(settings, null) { }

			public new bool ShouldConvert(Format settings) => base.ShouldConvert(settings);

			public async Task<ICollection<Mesg>> ConvertForTest(string path, Settings settings)
			{
				var workoutData = fileHandler.DeserializeJson<P2GWorkout>(path);
				var converted = await this.ConvertInternalAsync(workoutData.Workout, workoutData.WorkoutSamples, workoutData.UserData, settings);

				return converted.Item2;
			}

			public DeviceInfoMesg GetDeviceInfo(GarminDeviceInfo deviceInfo, Dynastream.Fit.DateTime startTime)
			{
				return base.GetDeviceInfoMesg(deviceInfo, startTime);
			}
		}
	}
}
