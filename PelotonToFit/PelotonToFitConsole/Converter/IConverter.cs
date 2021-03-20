using Common.Dto;
using System;
using System.Linq;

namespace PelotonToFitConsole.Converter
{
	public interface IConverter
	{
		public void Convert();
		public void Decode(string filePath);
	}

	public abstract class Converter : IConverter
	{
		private static readonly float _metersPerMile = 1609.34f;

		public abstract void Convert();
		public abstract void Decode(string filePath);

		protected DateTime GetStartTime(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(startTimeInSeconds);
			return dtDateTime.ToUniversalTime();
		}

		protected string GetTimeStamp(DateTime startTime, long offset = 0)
		{
			return startTime.AddSeconds(offset).ToString("yyyy-MM-ddTHH:mm:ssZ");
		}

		protected float ConvertDistanceToMeters(double value, string unit)
		{
			switch (unit)
			{
				case "km":
					return (float)value * 1000;
				case "mi":
					return (float)value * _metersPerMile;
				case "ft":
					return (float)value * 0.3048f;
				default:
					return (float)value;
			}
		}

		protected float GetTotalDistance(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
				return 0.0f;

			var unit = distanceSummary.Display_Unit;
			return ConvertDistanceToMeters(distanceSummary.Value, unit);
		}

		protected float ConvertToMetersPerSecond(double value, WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Summaries;
			var distanceSummary = summaries.FirstOrDefault(s => s.Slug == "distance");
			if (distanceSummary is null)
				return (float)value;

			var unit = distanceSummary.Display_Unit;
			var metersPerHour = ConvertDistanceToMeters(value, unit);
			var metersPerMinute = metersPerHour / 60;
			var metersPerSecond = metersPerMinute / 60;

			return metersPerSecond;
		}

		protected float GetMaxSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Metrics;
			var speedSummary = summaries.FirstOrDefault(s => s.Slug == "speed");
			if (speedSummary is null)
				return 0.0f;

			return ConvertToMetersPerSecond(speedSummary.Max_Value, workoutSamples);
		}

		protected float GetAvgSpeedMetersPerSecond(WorkoutSamples workoutSamples)
		{
			var summaries = workoutSamples.Metrics;
			var speedSummary = summaries.FirstOrDefault(s => s.Slug == "speed");
			if (speedSummary is null)
				return 0.0f;

			return ConvertToMetersPerSecond(speedSummary.Average_Value, workoutSamples);
		}

		protected string GetTitle(Workout workout)
		{
			return $"{workout.Ride.Title} with {workout.Ride.Instructor.Name}"
				.Replace(" ", "_")
				.Replace("/", "-")
				.Replace(":", "-");
		}
	}
}
