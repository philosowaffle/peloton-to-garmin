using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Service;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Conversion;

public class ElevationGainCalculatorService : IElevationGainCalculatorService
{
	private static readonly ILogger _logger = LogContext.ForClass<ElevationGainCalculatorService>();
	
	private readonly ISettingsService _settingsService;

	public ElevationGainCalculatorService(ISettingsService settingsService)
	{
		_settingsService = settingsService;
	}

	public Task<float?> CalculateElevationGainAsync(Workout workout, WorkoutSamples workoutSamples, ElevationGainSettings settings)
	{
		if (!settings.CalculateElevationGain)
		{
			_logger.Debug("Elevation gain calculation is disabled in settings");
			return Task.FromResult<float?>(null);
		}

		var elevationGain = CalculateResistanceBasedElevationGain(workoutSamples, settings);
		if (elevationGain.HasValue)
		{
			_logger.Information("Calculated elevation gain: {ElevationGain}m", elevationGain.Value);
		}
		else
		{
			_logger.Debug("Elevation gain calculation failed: no resistance or speed data available");
		}

		return Task.FromResult(elevationGain);
	}

	public float? CalculateResistanceBasedElevationGain(WorkoutSamples workoutSamples, ElevationGainSettings settings)
	{
		if (workoutSamples?.Metrics == null)
		{
			_logger.Debug("No workout metrics available for elevation gain calculation");
			return null;
		}

		// Look for resistance metrics
		var resistanceMetrics = workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "resistance");
		if (resistanceMetrics?.Values == null)
		{
			_logger.Debug("No resistance metrics found in workout data");
			return null;
		}

		// Look for speed metrics
		var speedMetrics = GetSpeedSummary(workoutSamples);
		if (speedMetrics?.Values == null)
		{
			_logger.Debug("No speed metrics found in workout data");
			return null;
		}

		float totalElevationGain = 0f;
		var flatRoadResistance = settings.FlatRoadResistance;
		var maxGrade = settings.MaxGradePercentage;

		// Process each second of data
		for (int i = 0; i < resistanceMetrics.Values.Length && i < speedMetrics.Values.Length; i++)
		{
			var resistance = (float)resistanceMetrics.GetValue(i);
			var speedMps = ConvertSpeedToMetersPerSecond(speedMetrics.GetValue(i), speedMetrics.Display_Unit);

			// Only calculate elevation gain if we're "climbing" (resistance > flat road)
			if (resistance > flatRoadResistance)
			{
				var gradePercentage = CalculateGradeFromResistance(resistance, flatRoadResistance, maxGrade);
				var elevationGainThisSecond = speedMps * gradePercentage / 100f;
				totalElevationGain += elevationGainThisSecond;
			}
		}

		_logger.Debug("Calculated elevation gain: {TotalElevationGain}m from {DataPoints} data points", 
			totalElevationGain, Math.Min(resistanceMetrics.Values.Length, speedMetrics.Values.Length));

		return totalElevationGain;
	}

	private float CalculateGradeFromResistance(float resistance, float flatRoadResistance, float maxGrade)
	{
		if (resistance <= flatRoadResistance)
			return 0f;
			
		// Linear interpolation: resistance above flat road maps to grade 0-maxGrade%
		var resistanceRange = 100f - flatRoadResistance;
		var resistanceAboveFlat = resistance - flatRoadResistance;
		var gradePercentage = (resistanceAboveFlat / resistanceRange) * maxGrade;
		
		return Math.Min(gradePercentage, maxGrade); // Cap at max grade
	}

	private float ConvertSpeedToMetersPerSecond(double speed, string displayUnit)
	{
		if (speed <= 0) return 0f;

		switch (displayUnit?.ToLower())
		{
			case "mph":
				// Convert mph to m/s: 1 mph = 0.44704 m/s
				return (float)(speed * 0.44704);
			case "kph":
			case "km/h":
				// Convert km/h to m/s: 1 km/h = 0.277778 m/s
				return (float)(speed * 0.277778);
			case "m/s":
				return (float)speed;
			default:
				_logger.Warning("Unknown speed unit: {Unit}, treating as m/s", displayUnit);
				return (float)speed;
		}
	}

	private Metric GetSpeedSummary(WorkoutSamples workoutSamples)
	{
		if (workoutSamples?.Metrics == null)
			return null;

		// Look for speed metrics
		var speed = workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "speed");
		if (speed != null)
			return speed;

		// Fall back to split_pace for rowing
		return workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "split_pace");
	}
}