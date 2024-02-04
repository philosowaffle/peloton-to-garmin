using Common;
using Common.Database;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Dto.Garmin;
using Common.Service;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.Common.Service;
#pragma warning disable CS0618 // Type or member is obsolete
public class SettingServiceTests
{
	[Test]
	public async Task GetCustomDeviceInfoAsync_Chooses_LegacyDeviceFile_First()
	{
		// SETUP
		var mocker = new AutoMocker();
		var settingsService = mocker.CreateInstance<SettingsService>();


		var settings = new Settings()
		{
			Format = new Format()
			{
				DeviceInfoPath = "./some/path/to.xml",
				DeviceInfoSettings = Format.DefaultDeviceInfoSettings
			}
		};
		mocker.GetMock<ISettingsDb>().Setup(x => x.GetSettingsAsync(It.IsAny<int>())).ReturnsAsync(settings);

		var userDeviceInfo = new GarminDeviceInfo() 
		{
			Name = "ThisDevice"
		};
		mocker.GetMock<IFileHandling>().Setup(x => x.TryDeserializeXml<GarminDeviceInfo>("./some/path/to.xml", out userDeviceInfo)).Returns(true);

		// ACT
		var chosenDeviceInfo = await settingsService.GetCustomDeviceInfoAsync(null);

		// ASSERT
		chosenDeviceInfo.Name.Should().Be("ThisDevice", because: "If the user still has a device file registered then we should honor that to stay backwards compatible.");
	}

	[Test]
	public async Task GetCustomDeviceInfoAsync_When_LegacyDeviceFile_Fails_FallBackTo_NewSettings_IfAvailable()
	{
		// SETUP
		var mocker = new AutoMocker();
		var settingsService = mocker.CreateInstance<SettingsService>();

		var settings = new Settings()
		{
			Format = new Format()
			{
				DeviceInfoPath = "./some/path/to.xml",
				DeviceInfoSettings = new Dictionary<WorkoutType, GarminDeviceInfo>() { { WorkoutType.None, GarminDevices.TACXDevice } }
			}
		};
		mocker.GetMock<ISettingsDb>().Setup(x => x.GetSettingsAsync(It.IsAny<int>())).ReturnsAsync(settings);

		GarminDeviceInfo userDeviceInfo = null;
		mocker.GetMock<IFileHandling>().Setup(x => x.TryDeserializeXml<GarminDeviceInfo>("./some/path/to.xml", out userDeviceInfo)).Returns(false);

		// ACT
		var chosenDeviceInfo = await settingsService.GetCustomDeviceInfoAsync(null);

		// ASSERT
		chosenDeviceInfo.Name.Should().Be("TacxTrainingAppWin", because: "If the legacy device fails to deserialize then we should fall back to the new Settings.");
	}

	[Test]
	public async Task GetCustomDeviceInfoAsync_When_LegacyDeviceFile_Fails_And_NoNewSettings_FallsBackToDefaults()
	{
		// SETUP
		var mocker = new AutoMocker();
		var settingsService = mocker.CreateInstance<SettingsService>();

		var settings = new Settings()
		{
			Format = new Format()
			{
				DeviceInfoPath = "./some/path/to.xml",
			}
		};
		mocker.GetMock<ISettingsDb>().Setup(x => x.GetSettingsAsync(It.IsAny<int>())).ReturnsAsync(settings);

		GarminDeviceInfo userDeviceInfo = null;
		mocker.GetMock<IFileHandling>().Setup(x => x.TryDeserializeXml<GarminDeviceInfo>("./some/path/to.xml", out userDeviceInfo)).Returns(false);

		// ACT
		var chosenDeviceInfo = await settingsService.GetCustomDeviceInfoAsync(null);

		// ASSERT
		chosenDeviceInfo.Name.Should().Be("Forerunner 945", because: "If all fails we should fall back to the default Settings.");
	}

	[Test]
	public async Task GetCustomDeviceInfoAsync_Choose_CorrectDevice_For_WorkoutType([Values] WorkoutType workoutType)
	{
		// SETUP
		var mocker = new AutoMocker();
		var settingsService = mocker.CreateInstance<SettingsService>();

		var deviceInfoSettings = new Dictionary<WorkoutType, GarminDeviceInfo>()
				{
					{ WorkoutType.None, new GarminDeviceInfo() { Name = "MyDefaultDevice" } },
					{ WorkoutType.Circuit, GarminDevices.Forerunner945 },
					{ WorkoutType.Cycling, GarminDevices.TACXDevice },
					{ WorkoutType.Meditation, GarminDevices.EpixDevice },
				};

		var settings = new Settings()
		{
			Format = new Format()
			{
				DeviceInfoSettings = deviceInfoSettings
			}
		};
		mocker.GetMock<ISettingsDb>().Setup(x => x.GetSettingsAsync(It.IsAny<int>())).ReturnsAsync(settings);

		GarminDeviceInfo userDeviceInfo = null;
		mocker.GetMock<IFileHandling>().Setup(x => x.TryDeserializeXml<GarminDeviceInfo>("./some/path/to.xml", out userDeviceInfo)).Returns(false);

		// ACT
		var workout = new Workout
		{
			Fitness_Discipline = workoutType.ToFitnessDiscipline().fitnessDiscipline,
			Is_Outdoor = workoutType.ToFitnessDiscipline().isOutdoor,
		};
		var chosenDeviceInfo = await settingsService.GetCustomDeviceInfoAsync(workout);

		// ASSERT
		if (deviceInfoSettings.TryGetValue(workoutType, out var expectedDeviceInfo))
		{
			chosenDeviceInfo.Should().Be(expectedDeviceInfo);
		} else
		{
			chosenDeviceInfo.Should().Be(deviceInfoSettings[WorkoutType.None]);
		}
	}
}
#pragma warning restore CS0618 // Type or member is obsolete