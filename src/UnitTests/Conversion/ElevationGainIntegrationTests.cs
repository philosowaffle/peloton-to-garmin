using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Service;
using Conversion;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace UnitTests.Conversion;

public class ElevationGainIntegrationTests
{
	private readonly Mock<ISettingsService> _settingsServiceMock;
	private readonly Mock<IFileHandling> _fileHandlerMock;
	private readonly Mock<IElevationGainCalculatorService> _elevationGainCalculatorServiceMock;

	public ElevationGainIntegrationTests()
	{
		_settingsServiceMock = new Mock<ISettingsService>();
		_fileHandlerMock = new Mock<IFileHandling>();
		_elevationGainCalculatorServiceMock = new Mock<IElevationGainCalculatorService>();
	}

	[Fact]
	public async Task FitConverter_WithElevationGainEnabled_CalculatesElevationGain()
	{
		// Arrange
		var converter = new FitConverter(_settingsServiceMock.Object, _fileHandlerMock.Object, _elevationGainCalculatorServiceMock.Object);

		var workout = new Workout
		{
			Id = "test-workout",
			Start_Time = 1640995200, // Jan 1, 2022
			Fitness_Discipline = FitnessDiscipline.Cycling
		};

		var workoutSamples = new WorkoutSamples
		{
			Duration = 3600, // 1 hour
			Summaries = new List<Summary>(),
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 200 } // 200 watts average
			},
			Seconds_Since_Pedaling_Start = Enumerable.Range(0, 3600).ToList(),
			Segment_List = new List<Segment>()
		};

		var workoutData = new P2GWorkout
		{
			Workout = workout,
			WorkoutSamples = workoutSamples,
			UserData = new UserData()
		};

		var settings = new Settings
		{
			Format = new Format
			{
				Fit = true,
				Cycling = new Cycling
				{
					ElevationGain = new ElevationGainSettings
					{
						CalculateElevationGain = true,
						UserMassKg = 70f,
						GravityAcceleration = 9.81f
					}
				}
			}
		};

		_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
		_settingsServiceMock.Setup(s => s.GetCustomDeviceInfoAsync(It.IsAny<Workout>()))
			.ReturnsAsync(Format.DefaultDeviceInfoSettings[WorkoutType.None]);

		_elevationGainCalculatorServiceMock
			.Setup(s => s.CalculateElevationGainAsync(workout, workoutSamples, settings.Format.Cycling.ElevationGain))
			.ReturnsAsync(1500f); // 1500 meters elevation gain

		// Act
		var result = await converter.ConvertAsync(workoutData);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().Be(ConversionResult.Success);
		
		// Verify that elevation gain calculator was called
		_elevationGainCalculatorServiceMock.Verify(
			s => s.CalculateElevationGainAsync(workout, workoutSamples, settings.Format.Cycling.ElevationGain),
			Times.Once);
	}

	[Fact]
	public async Task FitConverter_WithElevationGainDisabled_DoesNotCalculateElevationGain()
	{
		// Arrange
		var converter = new FitConverter(_settingsServiceMock.Object, _fileHandlerMock.Object, _elevationGainCalculatorServiceMock.Object);

		var workout = new Workout
		{
			Id = "test-workout",
			Start_Time = 1640995200, // Jan 1, 2022
			Fitness_Discipline = FitnessDiscipline.Cycling
		};

		var workoutSamples = new WorkoutSamples
		{
			Duration = 3600, // 1 hour
			Summaries = new List<Summary>(),
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 200 } // 200 watts average
			},
			Seconds_Since_Pedaling_Start = Enumerable.Range(0, 3600).ToList(),
			Segment_List = new List<Segment>()
		};

		var workoutData = new P2GWorkout
		{
			Workout = workout,
			WorkoutSamples = workoutSamples,
			UserData = new UserData()
		};

		var settings = new Settings
		{
			Format = new Format
			{
				Fit = true,
				Cycling = new Cycling
				{
					ElevationGain = new ElevationGainSettings
					{
						CalculateElevationGain = false, // Disabled
						UserMassKg = 70f,
						GravityAcceleration = 9.81f
					}
				}
			}
		};

		_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
		_settingsServiceMock.Setup(s => s.GetCustomDeviceInfoAsync(It.IsAny<Workout>()))
			.ReturnsAsync(Format.DefaultDeviceInfoSettings[WorkoutType.None]);

		// Act
		var result = await converter.ConvertAsync(workoutData);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().Be(ConversionResult.Success);

		// Verify that elevation gain calculator was NOT called when disabled
		_elevationGainCalculatorServiceMock.Verify(
			s => s.CalculateElevationGainAsync(It.IsAny<Workout>(), It.IsAny<WorkoutSamples>(), It.IsAny<ElevationGainSettings>()),
			Times.Never);
	}

	[Fact]
	public async Task FitConverter_WithExistingElevationData_DoesNotCalculateElevationGain()
	{
		// Arrange
		var converter = new FitConverter(_settingsServiceMock.Object, _fileHandlerMock.Object, _elevationGainCalculatorServiceMock.Object);

		var workout = new Workout
		{
			Id = "test-workout",
			Start_Time = 1640995200, // Jan 1, 2022
			Fitness_Discipline = FitnessDiscipline.Cycling
		};

		var workoutSamples = new WorkoutSamples
		{
			Duration = 3600, // 1 hour
			Summaries = new List<Summary>
			{
				new Summary { Slug = "elevation", Value = 800, Display_Unit = "m" } // Existing elevation data
			},
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 200 } // 200 watts average
			},
			Seconds_Since_Pedaling_Start = Enumerable.Range(0, 3600).ToList(),
			Segment_List = new List<Segment>()
		};

		var workoutData = new P2GWorkout
		{
			Workout = workout,
			WorkoutSamples = workoutSamples,
			UserData = new UserData()
		};

		var settings = new Settings
		{
			Format = new Format
			{
				Fit = true,
				Cycling = new Cycling
				{
					ElevationGain = new ElevationGainSettings
					{
						CalculateElevationGain = true, // Enabled but should not be used due to existing data
						UserMassKg = 70f,
						GravityAcceleration = 9.81f
					}
				}
			}
		};

		_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);
		_settingsServiceMock.Setup(s => s.GetCustomDeviceInfoAsync(It.IsAny<Workout>()))
			.ReturnsAsync(Format.DefaultDeviceInfoSettings[WorkoutType.None]);

		// Act
		var result = await converter.ConvertAsync(workoutData);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().Be(ConversionResult.Success);

		// Verify that elevation gain calculator was NOT called when elevation data already exists
		_elevationGainCalculatorServiceMock.Verify(
			s => s.CalculateElevationGainAsync(It.IsAny<Workout>(), It.IsAny<WorkoutSamples>(), It.IsAny<ElevationGainSettings>()),
			Times.Never);
	}
}