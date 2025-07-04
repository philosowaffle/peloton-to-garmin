using Common.Dto;
using Common.Dto.Peloton;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Service;

public class ElevationGainCalculatorService : IElevationGainCalculatorService
{
	private static readonly ILogger _logger = LogContext.ForClass<ElevationGainCalculatorService>();
	private const float CaloriesToJoulesMultiplier = 4184f; // 1 kcal = 4184 J
	
	private readonly ISettingsService _settingsService;

	public ElevationGainCalculatorService(ISettingsService settingsService)
	{
		_settingsService = settingsService;
	}

	public async Task<float?> CalculateElevationGainAsync(Workout workout, WorkoutSamples workoutSamples, ElevationGainSettings settings)
	{
		if (!settings.CalculateElevationGain)
		{
			_logger.Debug("Elevation gain calculation is disabled in settings");
			return null;
		}

		var userMass = await GetUserMassKgAsync(workout, settings);
		if (userMass == null)
		{
			_logger.Debug("Cannot calculate elevation gain: user mass not available");
			return null;
		}

		var energyInJoules = GetEnergyInJoules(workoutSamples);
		if (energyInJoules == null)
		{
			_logger.Debug("Cannot calculate elevation gain: energy data not available");
			return null;
		}

		// Formula: Elevation (m) = Energy (J) / (Mass (kg) x Gravity (m/s²))
		var elevationGain = energyInJoules.Value / (userMass.Value * settings.GravityAcceleration);
		
		_logger.Information("Calculated elevation gain: {ElevationGain}m from {Energy}J with {Mass}kg mass", 
			elevationGain, energyInJoules.Value, userMass.Value);

		return elevationGain;
	}

	public async Task<float?> GetUserMassKgAsync(Workout workout, ElevationGainSettings settings)
	{
		// First check if user provided mass in settings
		if (settings.UserMassKg.HasValue)
		{
			_logger.Debug("Using user-provided mass: {Mass}kg", settings.UserMassKg.Value);
			return settings.UserMassKg.Value;
		}

		// TODO: In the future, we could try to get user mass from Peloton or Garmin user profile data
		// For now, return null if not provided in settings
		_logger.Debug("No user mass provided in settings and external data retrieval not implemented");
		return null;
	}

	private float? GetEnergyInJoules(WorkoutSamples workoutSamples)
	{
		if (workoutSamples?.Summaries == null)
		{
			_logger.Debug("No workout summaries available for energy calculation");
			return null;
		}

		// Look for calories summary (preferred)
		var caloriesSummary = workoutSamples.Summaries.FirstOrDefault(s => s.Slug == "calories");
		if (caloriesSummary == null)
		{
			// Look for total_calories as fallback (Apple Watch integration)
			caloriesSummary = workoutSamples.Summaries.FirstOrDefault(s => s.Slug == "total_calories");
		}

		if (caloriesSummary == null)
		{
			_logger.Debug("No calories summary found in workout data");
			return null;
		}

		if (!caloriesSummary.Value.HasValue)
		{
			_logger.Debug("Calories summary has no value");
			return null;
		}

		var energyValue = caloriesSummary.Value.Value;
		var displayUnit = caloriesSummary.Display_Unit ?? "kcal";

		// Convert to Joules based on unit
		float energyInJoules = displayUnit.ToLowerInvariant() switch
		{
			"kcal" => energyValue * CaloriesToJoulesMultiplier,
			"cal" => energyValue * CaloriesToJoulesMultiplier / 1000f, // cal to kcal to J
			"j" => energyValue,
			"joule" => energyValue,
			"joules" => energyValue,
			_ => energyValue * CaloriesToJoulesMultiplier // Default to kcal assumption
		};

		_logger.Debug("Converted energy: {EnergyValue} {Unit} = {EnergyInJoules}J", 
			energyValue, displayUnit, energyInJoules);

		return energyInJoules;
	}
}