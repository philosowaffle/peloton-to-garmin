using Serilog;

namespace Common.Dto;

public enum DistanceUnit : byte
{
	Unknown = 0,
	Kilometers = 1,
	Miles = 2,
	Feet = 3,
	Meters = 4,
	FiveHundredMeters = 5,
}

public enum SpeedUnit : byte
{
	Unknown = 0,
	KilometersPerHour = 1,
	MilesPerHour = 2,
	MinutesPer500Meters = 3,
}

public enum WeightUnit : byte
{
	Unknown = 0,
	Pounds = 1,
	Kilograms = 2
}

public static class UnitHelpers
{
	public static DistanceUnit GetDistanceUnit(string unit)
	{
		switch (unit?.ToLower())
		{
			case "km":
			case "kph":
				return DistanceUnit.Kilometers;
			case "m":
				return DistanceUnit.Meters;
			case "mi":
			case "mph":
				return DistanceUnit.Miles;
			case "ft":
				return DistanceUnit.Feet;
			case "min/500m":
				return DistanceUnit.FiveHundredMeters;
			default:
				Log.Error("Found unknown distance unit {@Unit}", unit);
				return DistanceUnit.Unknown;
		}
	}

	public static SpeedUnit GetSpeedUnit(string unit)
	{
		switch (unit?.ToLower())
		{
			case "kph":
				return SpeedUnit.KilometersPerHour;
			case "mph":
				return SpeedUnit.MilesPerHour;
			case "min/500m":
				return SpeedUnit.MinutesPer500Meters;
			default:
				Log.Error("Found unknown distance unit {@Unit}", unit);
				return SpeedUnit.Unknown;
		}
	}

	public static WeightUnit GetWeightUnit(string unit)
	{
		switch(unit?.ToLower())
		{
			case "lb": return WeightUnit.Pounds;
			case "kg": return WeightUnit.Kilograms;
			default:
				Log.Error("Found unknown distance unit {@Unit}", unit);
				return WeightUnit.Unknown;
		}
	}
}