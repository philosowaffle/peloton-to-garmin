using Common.Dto;
using Common.Dto.Peloton;
using Common.Service;
using Conversion;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.Conversion;

public class ElevationGainCalculatorServiceTests
{
	private readonly Mock<ISettingsService> _settingsServiceMock;
	private readonly ElevationGainCalculatorService _service;

	public ElevationGainCalculatorServiceTests()
	{
		_settingsServiceMock = new Mock<ISettingsService>();
		_service = new ElevationGainCalculatorService(_settingsServiceMock.Object);
	}

	[Test]
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
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

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
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

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
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

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
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

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
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeGreaterThan(0f);
		// Only resistance 35 and 40 should contribute to elevation gain
		// Resistance 25 and 30 should be ignored (below or equal to flat road)
	}

	[Test]
	public async Task CalculateElevationGainAsync_WithCustomFlatRoadResistance_CalculatesCorrectly()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Metrics = new List<Metric>
			{
				new Metric { Slug = "resistance", Values = new double?[] { 35, 40 } }, // Above custom flat road (25)
				new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 15, 12 } }
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			FlatRoadResistance = 25f, // Custom flat road resistance
			MaxGradePercentage = 15f
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeGreaterThan(0f);
		// Resistance 35: Grade = 15% * (10/75) = 2%, Speed = 15 mph = 6.7 m/s, Elevation = 6.7 * 2% = 0.134 m
		// Resistance 40: Grade = 15% * (15/75) = 3%, Speed = 12 mph = 5.36 m/s, Elevation = 5.36 * 3% = 0.161 m
		// Total: 0.134 + 0.161 = 0.295 m
		result.Should().BeApproximately(0.295f, 0.01f);
	}

	[Test]
	public async Task CalculateElevationGainAsync_WithCustomMaxGrade_CapsGradeCorrectly()
	{
		// Arrange
		var workout = new Workout();
		var workoutSamples = new WorkoutSamples
		{
			Metrics = new List<Metric>
			{
				new Metric { Slug = "resistance", Values = new double?[] { 100 } }, // Maximum resistance
				new Metric { Slug = "speed", Display_Unit = "mph", Values = new double?[] { 10 } }
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			FlatRoadResistance = 30f,
			MaxGradePercentage = 10f // Custom max grade (should cap at 10% instead of 15%)
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeGreaterThan(0f);
		// Resistance 100: Grade should be capped at 10%, Speed = 10 mph = 4.47 m/s, Elevation = 4.47 * 10% = 0.447 m
		result.Should().BeApproximately(0.447f, 0.01f);
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
				new Metric { Slug = "resistance", Values = new double?[] { 40 } },
				new Metric { Slug = "speed", Display_Unit = "km/h", Values = new double?[] { 20 } } // 20 km/h
			}
		};
		var settings = new ElevationGainSettings 
		{ 
			CalculateElevationGain = true,
			FlatRoadResistance = 30f,
			MaxGradePercentage = 15f
		};

		// Act
		var result = await _service.CalculateElevationGainAsync(workout, workoutSamples, settings);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeGreaterThan(0f);
		// Resistance 40: Grade = 15% * (10/70) = 2.14%, Speed = 20 km/h = 5.56 m/s, Elevation = 5.56 * 2.14% = 0.119 m
		result.Should().BeApproximately(0.119f, 0.01f);
	}

	// Direct method tests
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
		var result = _service.CalculateResistanceBasedElevationGain(workoutSamples, settings);

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
		var result = _service.CalculateResistanceBasedElevationGain(workoutSamples, settings);

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
		var result = _service.CalculateResistanceBasedElevationGain(workoutSamples, settings);

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
		var result = _service.CalculateResistanceBasedElevationGain(workoutSamples, settings);

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
}