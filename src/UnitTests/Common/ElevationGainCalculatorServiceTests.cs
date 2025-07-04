using Common.Dto;
using Common.Dto.Peloton;
using Common.Service;
using FluentAssertions;
using Moq;
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
	public async Task CalculateElevationGainAsync_WhenNoEnergyData_ReturnsNull()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples { Summaries = new List<Summary>() };
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
			Summaries = new List<Summary>
			{
				new Summary { Slug = "calories", Value = 300, Display_Unit = "kcal" }
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
			Summaries = new List<Summary>
			{
				new Summary { Slug = "calories", Value = 300, Display_Unit = "kcal" }
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
		// Energy = 300 kcal = 300 * 4184 J = 1,255,200 J
		// Elevation = 1,255,200 J / (70 kg * 9.81 m/s²) = 1,255,200 / 686.7 = ~1828 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(1828f, 1f);
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithTotalCaloriesSlug_CalculatesCorrectElevation()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Summaries = new List<Summary>
			{
				new Summary { Slug = "total_calories", Value = 250, Display_Unit = "kcal" }
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
		// Energy = 250 kcal = 250 * 4184 J = 1,046,000 J
		// Elevation = 1,046,000 J / (80 kg * 9.81 m/s²) = 1,046,000 / 784.8 = ~1333 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(1333f, 1f);
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithJoulesUnit_CalculatesCorrectElevation()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Summaries = new List<Summary>
			{
				new Summary { Slug = "calories", Value = 1000000, Display_Unit = "J" }
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
		// Energy = 1,000,000 J
		// Elevation = 1,000,000 J / (70 kg * 9.81 m/s²) = 1,000,000 / 686.7 = ~1456 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(1456f, 1f);
	}

	[Fact]
	public async Task CalculateElevationGainAsync_WithCustomGravity_CalculatesCorrectElevation()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Summaries = new List<Summary>
			{
				new Summary { Slug = "calories", Value = 200, Display_Unit = "kcal" }
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
		// Energy = 200 kcal = 200 * 4184 J = 836,800 J
		// Elevation = 836,800 J / (75 kg * 10 m/s²) = 836,800 / 750 = ~1116 m
		result.Should().NotBeNull();
		result.Should().BeApproximately(1116f, 1f);
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