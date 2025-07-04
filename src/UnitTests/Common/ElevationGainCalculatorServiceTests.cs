using Common.Dto;
using Common.Dto.Peloton;
using Common.Service;
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace UnitTests.Common;

public class ElevationGainCalculatorServiceTests
{
	private readonly Mock<ISettingsService> _settingsServiceMock;
	private readonly ElevationGainCalculatorService _service;

	public ElevationGainCalculatorServiceTests()
	{
		_settingsServiceMock = new Mock<ISettingsService>();
		_service = new ElevationGainCalculatorService(_settingsServiceMock.Object);
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WhenSettingsDisabled_ReturnsNull()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples();
		var settings = new ElevationGainSettings { CalculateElevationGain = false };

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WhenNoPowerData_ReturnsNull()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples 
		{ 
			Metrics = new List<Metric>(),
			Duration = 3600
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			UserMassKg = 70f 
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WhenNoUserMass_ReturnsNull()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Duration = 3600, // 1 hour
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 200 } // 200 watts average
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			UserMassKg = null
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithValidData_CalculatesCorrectElevation()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Duration = 3600, // 1 hour
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 200 } // 200 watts average
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			UserMassKg = 70f,
			GravityAcceleration = 9.81f
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		// Energy = 200W × 3600s = 720,000 J
		// Elevation = 720,000 J / (70 kg × 9.81 m/s²) = 720,000 / 686.7 = ~1048 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(1048f, 1f);
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithDifferentPowerAndDuration_CalculatesCorrectElevation()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Duration = 1800, // 30 minutes
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 150 } // 150 watts average
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			UserMassKg = 80f,
			GravityAcceleration = 9.81f
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		// Energy = 150W × 1800s = 270,000 J
		// Elevation = 270,000 J / (80 kg × 9.81 m/s²) = 270,000 / 784.8 = ~344 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(344f, 1f);
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithZeroPower_ReturnsNull()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Duration = 3600,
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 0 } // 0 watts average
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			UserMassKg = 70f,
			GravityAcceleration = 9.81f
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithCustomGravity_CalculatesCorrectElevation()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Duration = 2700, // 45 minutes
			Metrics = new List<Metric>
			{
				new Metric { Slug = "output", Average_Value = 180 } // 180 watts average
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			UserMassKg = 75f,
			GravityAcceleration = 10f // Custom gravity
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		// Energy = 180W × 2700s = 486,000 J
		// Elevation = 486,000 J / (75 kg × 10 m/s²) = 486,000 / 750 = 648 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(648f, 1f);
	}

	[Fact]
	public async Task GetUserMassKgAsync_WithSettingsProvided_ReturnsSettingsValue()
	{
		// Arrange
		var workout = new Workout();
		var settings = new ElevationGainSettings { UserMassKg = 75f };

		// Act
		var result = await _service.GetUserMassKgAsync(workout, settings);

		// Assert
		result.Should().Be(75f);
	}

	[Fact]
	public async Task GetUserMassKgAsync_WithNoSettingsButPelotonData_ReturnsPelotonValue()
	{
		// Arrange
		var workout = new Workout();
		var settings = new ElevationGainSettings { UserMassKg = null };

		// TODO: This test will need to be implemented when we add Peloton/Garmin user data retrieval
		// Act
		var result = await _service.GetUserMassKgAsync(workout, settings);

		// Assert
		result.Should().BeNull(); // For now, returns null when no settings provided
	}

	[Fact]
	public async Task GetUserMassKgAsync_WithNoData_ReturnsNull()
	{
		// Arrange
		var workout = new Workout();
		var settings = new ElevationGainSettings { UserMassKg = null };

		// Act
		var result = await _service.GetUserMassKgAsync(workout, settings);

		// Assert
		result.Should().BeNull();
	}
}