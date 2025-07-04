using Common.Dto;
using Common.Dto.Peloton;
using System.Threading.Tasks;

namespace Conversion;

public interface IElevationGainCalculatorService
{
	/// <summary>
	/// Calculates estimated elevation gain using resistance data to estimate grade.
	/// This method processes resistance data second-by-second to calculate elevation gain
	/// based on the relationship between resistance, speed, and grade.
	/// </summary>
	/// <param name="workout">The workout data</param>
	/// <param name="workoutSamples">The workout samples containing resistance and speed data</param>
	/// <param name="settings">The elevation gain settings</param>
	/// <returns>Estimated elevation gain in meters, or null if calculation cannot be performed</returns>
	Task<float?> CalculateElevationGainAsync(Workout workout, WorkoutSamples workoutSamples, ElevationGainSettings settings);

	/// <summary>
	/// Calculates estimated elevation gain using resistance data to estimate grade.
	/// This method processes resistance data second-by-second to calculate elevation gain
	/// based on the relationship between resistance, speed, and grade.
	/// </summary>
	/// <param name="workoutSamples">The workout samples containing resistance and speed data</param>
	/// <param name="settings">The elevation gain settings</param>
	/// <returns>Estimated elevation gain in meters, or null if calculation cannot be performed</returns>
	float? CalculateResistanceBasedElevationGain(WorkoutSamples workoutSamples, ElevationGainSettings settings);
}