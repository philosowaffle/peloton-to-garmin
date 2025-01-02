using Common.Dto;
using Common.Dto.Peloton;
using System.Collections.Generic;
using System.Linq;

namespace Sync;

public static class StackedClassesCalculator
{
	/// <summary>
	/// Organizes a list of workouts into stacks.  To qualify for a stack a workouts must:
	///  1. Be of the same Workout Type
	///  1. Start within X seconds (configurable) of the workout before it
	/// </summary>
	/// <param name="workouts"></param>
	/// <returns>A Dictionary of Stacks.  Each key is a stackId, and the value is a list of workouts belonging to that stack.</returns>
	public static Dictionary<int, List<P2GWorkout>> GetStackedClasses(ICollection<P2GWorkout> workouts)
	{
		// calculate stacked workouts
		// if (combineStackedWorkouts)
		var orderedAndGroupedWorkouts = workouts
								.OrderBy(w => w.Workout.Start_Time)
								.GroupBy(w => w.WorkoutType);

		var stacks = new Dictionary<int, List<P2GWorkout>>(); // <stackId, List of Workouts to stack>
		var currentStack = 0;
		var stackThreshold = 60; // 1min

		foreach (var workoutGrouping in orderedAndGroupedWorkouts)
		{
			currentStack++;
			stacks.Add(currentStack, new());

			foreach (var workout in workoutGrouping)
			{
				var lastWorkoutOfCurrentStack = stacks[currentStack].LastOrDefault();
				if (lastWorkoutOfCurrentStack is null)
				{
					stacks[currentStack].Add(workout);
					continue;
				}

				var timeBetweenEndAndStart = workout.Workout.Start_Time - lastWorkoutOfCurrentStack.Workout.End_Time;
				if (timeBetweenEndAndStart <= stackThreshold)
				{
					stacks[currentStack].Add(workout);
					continue;
				}

				currentStack++;
				stacks.Add(currentStack, new() { workout });
			}
		}

		return stacks;
	}

	public static ICollection<P2GWorkout> CombineStackedClasses(Dictionary<int, List<P2GWorkout>> stacks)
	{
		var stackedWorkouts = new List<P2GWorkout>();
		foreach (var stack in stacks)
		{
			var workoutsToStack = stack.Value;

			if (workoutsToStack is null) continue;

			// Only a single workout, so just add as is
			if (workoutsToStack.Count == 1)
			{
				var workout = workoutsToStack.FirstOrDefault();
				if (workout is null) continue;

				stackedWorkouts.Add(workout);
				continue;
			}

			var firstWorkout = workoutsToStack.FirstOrDefault();
			if (firstWorkout is null) continue;

			var lastWorkout = workoutsToStack.Last();

			var stackedWorkout = new P2GWorkout()
			{
				IsStackedWorkout = true,

				Workout = new Common.Dto.Peloton.Workout()
				{
					Name = string.Concat(workoutsToStack.Select(w => w.Workout.Name), ","),
					Title = string.Concat(workoutsToStack.Select(w => w.Workout.Title), ","),

					Created_At = firstWorkout.Workout.Created_At,
					Start_Time = firstWorkout.Workout.Start_Time,
					End_Time = lastWorkout.Workout.End_Time,

					Fitness_Discipline = firstWorkout.Workout.Fitness_Discipline,
					Ftp_Info = firstWorkout.Workout.Ftp_Info,
					Is_Outdoor = firstWorkout.Workout.Is_Outdoor,

					Movement_Tracker_Data = firstWorkout.Workout.Movement_Tracker_Data, // TODO

					Status = firstWorkout.Workout.Status,
					Total_Work = workoutsToStack.Sum(w => w.Workout.Total_Work),

					Ride = firstWorkout.Workout.Ride, // Currently only referencing Title and Instructor
				},

				WorkoutSamples = new Common.Dto.Peloton.WorkoutSamples()
				{
					Average_Summaries = new List<AverageSummary>(), // not used
					Duration = workoutsToStack.Sum(w => w.WorkoutSamples.Duration),
					Is_Class_Plan_Shown = false, // not used
					Has_Apple_Watch_Metrcis = false, // not used
					Is_Location_Data_Accurate = false, // not used
					Location_Data = CombineLocationData(workoutsToStack),
					Metrics = CombineMetricsData(workoutsToStack),
				}
			};

			stackedWorkouts.Add(stackedWorkout);
		}

		return stackedWorkouts;
	}

	public static ICollection<LocationData> CombineLocationData(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedLocationData = new List<LocationData>();
		var totalSecondsSoFar = 0;
		foreach (var workout in workoutsToStack)
		{
			var adjustedLocationData = workout.WorkoutSamples
										.Location_Data
										.Select(l =>
										{
											var adjustedCoords = l.Coordinates.Select(c =>
											{
												c.Seconds_Offset_From_Start += totalSecondsSoFar;
												return c;
											});

											l.Coordinates = adjustedCoords.ToList();
											return l;
										});

			stackedLocationData.AddRange(adjustedLocationData);
			totalSecondsSoFar += workout.WorkoutSamples.Duration;
		}

		return stackedLocationData;
	}

	public static ICollection<Metric> CombineMetricsData(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedMetricData = new List<Metric>();

		foreach (var workout in workoutsToStack)
		{
			foreach (var metric in workout.WorkoutSamples.Metrics)
			{
				var aggregateSlug = stackedMetricData.FirstOrDefault(s => s.Slug == metric.Slug);

				if (aggregateSlug is null)
				{
					aggregateSlug = new Metric()
					{
						Slug = metric.Slug,
						Display_Name = metric.Display_Name,
						Display_Unit = metric.Display_Unit,
						Max_Value = metric.Max_Value,
						Values = new double?[0],
						Zones = metric.Zones,
						Missing_Data_Duration = 0,
					};
					stackedMetricData.Add(aggregateSlug);
				}

				aggregateSlug.Values = aggregateSlug.Values.Union(metric.Values).ToArray();
				aggregateSlug.Missing_Data_Duration += metric.Missing_Data_Duration;
				aggregateSlug.Max_Value = aggregateSlug.Max_Value > metric.Max_Value ? aggregateSlug.Max_Value : metric.Max_Value;

				// zones
				foreach (var zone in metric.Zones)
				{
					var aggregateZoneSlug = aggregateSlug.Zones.FirstOrDefault(z => z.Slug == zone.Slug);

					if (aggregateZoneSlug is null)
					{
						aggregateSlug.Zones.Add(zone);
						continue;
					}

					aggregateZoneSlug.Duration += zone.Duration;
					aggregateZoneSlug.Max_Value = aggregateZoneSlug.Max_Value > zone.Max_Value ? aggregateZoneSlug.Max_Value : zone.Max_Value;
					aggregateZoneSlug.Min_Value = aggregateZoneSlug.Min_Value < zone.Min_Value ? aggregateZoneSlug.Min_Value : zone.Min_Value;
				}

				// Alternatives - Won't Support for MVP
			}
		}

		return stackedMetricData;
	}
}
