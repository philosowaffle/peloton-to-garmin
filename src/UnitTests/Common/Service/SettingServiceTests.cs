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

public class SettingServiceTests
{
	[Test]
	public async Task GetCustomDeviceInfoAsync_When_LegacyDeviceFile_Fails_And_NoNewSettings_FallsBackToDefaults()
	{
		// SETUP
		var mocker = new AutoMocker();
		var settingsService = mocker.CreateInstance<SettingsService>();

		var settings = new Settings();
		mocker.GetMock<ISettingsDb>().Setup(x => x.GetSettingsAsync(It.IsAny<int>())).ReturnsAsync(settings);

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