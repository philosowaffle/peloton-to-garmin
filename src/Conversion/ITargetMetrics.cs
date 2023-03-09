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

    public abstract TargetRange ToRange(Common.Dto.Peloton.PowerZones powerZones);

    public void ApplyToWorkoutStep(WorkoutStepMesg step)
    {
        var targetValue = TargetValue();
        var (low, high) = (CustomTargetValueLow(), CustomTargetValueHigh());
        step.SetTargetType(TargetType);
        step.SetIntensity(GetIntensity());
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
            if (high > 0)
            {
                // Target values need to be non-zero or Garmin ignores them
                step.SetCustomTargetPowerLow(Math.Max(low, 1));
                step.SetCustomTargetPowerHigh(high);
            }
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
        case "stroke_rate":
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

    public override TargetRange ToRange(Common.Dto.Peloton.PowerZones powerZones)
    {
        switch (TargetType) {
        case WktStepTarget.Power:
            Func<Common.Dto.Peloton.Zone, TargetRange> range = zone => new TargetRange
            {
                TargetType = TargetType,
                Low = (uint)zone.Min_Value,
                High = (uint)zone.Max_Value,
            };
            switch (Zone) {
            case 1: return range(powerZones.Zone1);
            case 2: return range(powerZones.Zone2);
            case 3: return range(powerZones.Zone3);
            case 4: return range(powerZones.Zone4);
            case 5: return range(powerZones.Zone5);
            case 6: return range(powerZones.Zone6);
            case 7: return range(powerZones.Zone7);
            default: throw new Exception("Invalid power zone: " + Zone);
            }
        default:
            throw new Exception("Unhandled zone type");
        }
    }

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
    public override TargetRange ToRange(Common.Dto.Peloton.PowerZones powerZones) { return this; }

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

    public static IEnumerable<TargetGraphMetrics> Extract(Common.Dto.Peloton.WorkoutSamples workoutSamples)
    {
        if (workoutSamples?.Target_Performance_Metrics?.Target_Graph_Metrics is null)
            return Enumerable.Empty<TargetGraphMetrics>();

        return workoutSamples.Target_Performance_Metrics.Target_Graph_Metrics
            .Select(metrics => new TargetGraphMetrics { metrics = metrics, targetType = Target.ParseTargetType(metrics.Type) });
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
    ICollection<int> secondsSincePedalingStart;
    ICollection<(Offsets offsets, Target target)> targets;

    public static IEnumerable<TargetMetrics> Extract(Common.Dto.Peloton.RideDetails rideDetails, Common.Dto.Peloton.WorkoutSamples workoutSamples)
    {
        if (rideDetails?.Ride is null || rideDetails?.Target_Metrics_Data?.Target_Metrics is null)
            return Enumerable.Empty<TargetMetrics>();

        var pedalingStartOffset = rideDetails.Ride.Pedaling_Start_Offset;
        return rideDetails.Target_Metrics_Data.Target_Metrics
            .SelectMany(metric => metric.Metrics.Select(target => (target, metric)))
            .GroupBy(x => x.target.Name) // TODO: does GroupBy preserve ordering?
            .Select(group =>
            {
                var (type, isZone) = Target.ParseTargetType(group.Key);
                return new TargetMetrics
                {
                    secondsSincePedalingStart = workoutSamples.Seconds_Since_Pedaling_Start,
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
        var offsets = secondsSincePedalingStart.Zip(secondsSincePedalingStart.Skip(1), (x, y) => y - x).GetEnumerator();

        var offset = 0;
        while (targets.MoveNext())
        {
            while (offset <= targets.Current.offsets.End)
            {
                if (offset >= targets.Current.offsets.Start)
                    yield return targets.Current.target;
                else
                    yield return null;

                if (offsets.MoveNext())
                    offset += offsets.Current;
                else
                    yield break;
            }
        }
	}
}