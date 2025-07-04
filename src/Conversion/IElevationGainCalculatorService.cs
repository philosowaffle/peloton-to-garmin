using Common.Dto;
using Common.Dto.Peloton;

namespace Conversion;

public interface IElevationGainCalculatorService
{
	/// <summary>
	/// Calculates estimated elevation gain from energy output using the formula:
	/// Elevation (m) = Energy (J) / (Mass (kg) x Gravity (m/s²))
	/// </summary>
	/// <param name="workout">The workout data containing energy information</param>
	/// <param name="workoutSamples">The workout samples containing energy values</param>
	/// <param name="settings">The elevation gain settings</param>
	/// <returns>Estimated elevation gain in meters, or null if calculation cannot be performed</returns>
	Task<float?> CalculateElevationGainAsync(Workout workout, WorkoutSamples workoutSamples, ElevationGainSettings settings);

	/// <summary>
	/// Gets the user's mass in kilograms from settings, Peloton, or Garmin data.
	/// </summary>
	/// <param name="workout">The workout data</param>
	/// <param name="settings">The elevation gain settings</param>
	/// <returns>User's mass in kilograms, or null if not available</returns>
	Task<float?> GetUserMassKgAsync(Workout workout, ElevationGainSettings settings);
}