using Dynastream.Fit;
using System.Collections.Generic;

namespace Conversion;

public abstract class Target
{
    public WktStepTarget TargetType;
	public abstract uint TargetValue();
	public abstract uint CustomTargetValueLow();
	public abstract uint CustomTargetValueHigh();

    public abstract Intensity GetIntensity();

    public void ApplyToWorkoutStep(WorkoutStepMesg step)
    {
        var targetValue = TargetValue();
        var (low, high) = (CustomTargetValueLow(), CustomTargetValueHigh());
        switch (TargetType) {
        case WktStepTarget.Cadence:
            step.SetTargetCadenceZone(targetValue);
            step.SetCustomTargetCadenceLow(low);
            step.SetCustomTargetCadenceHigh(high);
            break;
        case WktStepTarget.HeartRate:
            step.SetTargetHrZone(targetValue);
            step.SetCustomTargetHeartRateLow(low);
            step.SetCustomTargetHeartRateHigh(high);
            break;
        case WktStepTarget.Power:
            step.SetTargetPowerZone(targetValue);
            step.SetCustomTargetPowerLow(low);
            step.SetCustomTargetPowerHigh(high);
            break;
        case WktStepTarget.Speed:
            step.SetTargetSpeedZone(targetValue);
            step.SetCustomTargetSpeedLow(low);
            step.SetCustomTargetSpeedHigh(high);
            break;
        default:
            step.SetTargetValue(targetValue);
            step.SetCustomTargetValueLow(low);
            step.SetCustomTargetValueHigh(high);
            break;
        }
    }
}

public class TargetZone : Target
{
	public uint Zone { get; init; }
	public override uint TargetValue() { return Zone; }
	public override uint CustomTargetValueLow() { return 0; }
	public override uint CustomTargetValueHigh() { return 0; }

	public override Intensity GetIntensity()
	{
        switch (TargetType) {
        case WktStepTarget.Power:
            if (Zone == 1) return Intensity.Rest;
            else return Intensity.Active;
        case WktStepTarget.HeartRate:
            if (Zone == 1) return Intensity.Rest;
            else return Intensity.Active;
        default:
            return Intensity.Active;
        }
	}
}

public class TargetRange : Target
{
	public uint Low { get; init; }
	public uint High { get; init; }
	public override uint TargetValue() { return 0; }
	public override uint CustomTargetValueLow() { return Low; }
	public override uint CustomTargetValueHigh() { return High; }

	public override Intensity GetIntensity()
	{
        switch (TargetType) {
        case WktStepTarget.Resistance:
            if (High > 20) return Intensity.Active;
            else return Intensity.Rest;
        case WktStepTarget.Cadence:
            if (High > 60) return Intensity.Active;
            else return Intensity.Rest;
        default:
            return Intensity.Active;
        }
	}
}

public interface ITargetMetrics : IEnumerable<Target>
{

}