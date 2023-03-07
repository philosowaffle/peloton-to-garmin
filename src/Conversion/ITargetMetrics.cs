using Dynastream.Fit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Conversion;

public abstract class Target
{
    public WktStepTarget TargetType;
    public abstract uint TargetValue();
    public abstract uint CustomTargetValueLow();
    public abstract uint CustomTargetValueHigh();

    public abstract Intensity GetIntensity();

    public static Target New(WktStepTarget type, bool isZone, uint low, uint high)
    {
        if (isZone)
        {
            if (low != high) throw new Exception("Target cannot span multiple zones");
            return new TargetZone
            {
                TargetType = type,
                Zone = (uint)low,
            };
        }
        else
        {
            return new TargetRange
            {
                TargetType = type,
                Low = (uint)low, High = (uint)high
            };
        }
    }

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

    public static (WktStepTarget, bool isZone) ParseTargetType(string typeName) {
        switch (typeName) {
        case "resistance":
            return (WktStepTarget.Cadence, false);
        case "cadence":
            return (WktStepTarget.Cadence, false);
        case "power_zone":
            return (WktStepTarget.Power, true);
        default:
            return (WktStepTarget.Invalid, false);
        }
    } 

    public override bool Equals(object obj)
    {
        return Equals(obj as Target);
    }

    public bool Equals(Target other)
    {
        return other != null
            && TargetType == other.TargetType
            && TargetValue() == other.TargetValue()
            && CustomTargetValueLow() == other.CustomTargetValueLow()
            && CustomTargetValueHigh() == other.CustomTargetValueHigh();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TargetType, TargetValue(), CustomTargetValueLow(), CustomTargetValueHigh());
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

public interface ITargetMetrics
{
    public IEnumerator<Target> Get1sTargets();
}

public class TargetGraphMetrics : ITargetMetrics
{
    Common.Dto.Peloton.TargetGraphMetrics metrics;
    (WktStepTarget, bool isZone) targetType;

    public TargetGraphMetrics(Common.Dto.Peloton.TargetGraphMetrics metrics)
    {
        this.metrics = metrics;
        targetType = Target.ParseTargetType(metrics.Type);
    }

    public IEnumerator<Target> Get1sTargets()
    {
        var (low, high) = (metrics.Graph_Data.Lower, metrics.Graph_Data.Upper);
        return low.Zip(high, (low, high) => Target.New(targetType.Item1, targetType.isZone, (uint)low, (uint)high)).GetEnumerator();
    }
}

struct Offsets
{
    public int Start { get; init; }
    public int End { get; init; }
}

public class TargetMetrics : ITargetMetrics
{
    ICollection<(Offsets offsets, Target target)> targets;

    public static IEnumerable<ITargetMetrics> Extract(Common.Dto.Peloton.RideDetails rideDetails)
    {
        var pedalingStartOffset = rideDetails.Ride.Pedaling_Start_Offset;
        return rideDetails.Target_Metrics_Data.Target_Metrics
            .SelectMany(metric => metric.Metrics.Select(target => (target, metric)))
            .GroupBy(x => x.target.Name) // TODO: does GroupBy preserve ordering?
            .Select(group =>
            {
                var (type, isZone) = Target.ParseTargetType(group.Key);
                return new TargetMetrics
                {
                    targets = group.Select(x =>
                    {
                        var offsets = x.metric.Offsets;
                        var adjustedOffsets = new Offsets
                        {
                            Start = offsets.Start-pedalingStartOffset,
                            End = offsets.End-pedalingStartOffset,
                        };
                        var target = Target.New(type, isZone, (uint)x.target.Lower, (uint)x.target.Upper);
                        return (adjustedOffsets, target);
                    }).ToList(),
                };
            });
    }

	public IEnumerator<Target> Get1sTargets()
	{
        var targets = this.targets.GetEnumerator();

        var offset = 0;
        while (targets.MoveNext())
        {
            if (offset > targets.Current.offsets.End)
                if (!targets.MoveNext()) break;

            if (offset >= targets.Current.offsets.Start)
                yield return targets.Current.target;
            else
                yield return null;
            offset++;
        }
	}
}