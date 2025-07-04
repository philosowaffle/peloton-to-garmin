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

		var energyInJoules = GetEnergyFromPowerData(workoutSamples);
		if (energyInJoules == null)
		{
			_logger.Debug("Cannot calculate elevation gain: power/energy data not available");
			return null;
		}

		// Formula: Elevation (m) = Energy (J) / (Mass (kg) x Gravity (m/s²))
		// Energy (J) = Average Power (W) × Duration (s)
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

	private float? GetEnergyFromPowerData(WorkoutSamples workoutSamples)
	{
		if (workoutSamples?.Metrics == null)
		{
			_logger.Debug("No workout metrics available for power calculation");
			return null;
		}

		// Look for output (power/watts) metrics
		var outputMetrics = workoutSamples.Metrics.FirstOrDefault(m => m.Slug == "output");
		if (outputMetrics == null)
		{
			_logger.Debug("No output (power) metrics found in workout data");
			return null;
		}

		if (outputMetrics.Average_Value == null || outputMetrics.Average_Value <= 0)
		{
			_logger.Debug("Output metrics has no average value or invalid value");
			return null;
		}

		var averagePowerWatts = (float)outputMetrics.Average_Value.Value;
		var durationSeconds = workoutSamples.Duration;

		if (durationSeconds <= 0)
		{
			_logger.Debug("Workout duration is invalid");
			return null;
		}

		// Energy (J) = Power (W) × Time (s)
		var energyInJoules = averagePowerWatts * durationSeconds;

		_logger.Debug("Calculated energy: {AveragePower}W × {Duration}s = {EnergyInJoules}J", 
			averagePowerWatts, durationSeconds, energyInJoules);

		return energyInJoules;
	}
}