using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sync;

public static class StackedWorkoutsCalculator
{
	private static readonly ILogger _logger = LogContext.ForStatic(nameof(StackedWorkoutsCalculator));

	/// <summary>
	/// Organizes a list of workouts into stacks.  To qualify for a stack a workouts must:
	///  1. Be of the same Workout Type
	///  1. Start within X seconds (configurable) of the workout before it
	/// </summary>
	/// <param name="workouts">List of workouts that could be stacked.</param>
	/// <param name="stackedWorkoutsTimeGapSeconds">The maximum amount of time between workouts to allow in stack.</param>
	/// <returns>A Dictionary of Stacks.  Each key is a stackId, and the value is a list of workouts belonging to that stack.</returns>
	public static Dictionary<int, List<P2GWorkout>> GetStackedWorkouts(IEnumerable<P2GWorkout> workouts, long stackedWorkoutsTimeGapSeconds)
	{
		var stacks = new Dictionary<int, List<P2GWorkout>>(); // <stackId, List of Workouts to stack>
		var currentStack = -1;

		if (workouts is null || !workouts.Any())
			return stacks;

		var orderedAndGroupedWorkouts = workouts
								.Where(w => w.Workout is object)
								.OrderBy(w => w.Workout.Start_Time)
								.GroupBy(w => w.WorkoutType);

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
				if (timeBetweenEndAndStart <= stackedWorkoutsTimeGapSeconds)
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

	/// <summary>
	/// Given workouts grouped into stacks, combines those stacks into one unified workout each.
	/// </summary>
	/// <param name="stacks"></param>
	/// <returns>A list of workouts, one per stack.</returns>
	public static ICollection<P2GWorkout> CombineStackedWorkouts(Dictionary<int, List<P2GWorkout>> stacks)
	{
		var stackedWorkouts = new List<P2GWorkout>();

		if (stacks is null)
			return stackedWorkouts;

		foreach (var stack in stacks)
		{
			try
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
				if (firstWorkout is null || firstWorkout.Workout is null) continue;

				var lastWorkout = workoutsToStack.Last();
				if (lastWorkout is null || lastWorkout.Workout is null) continue;

				var stackedWorkout = new P2GWorkout()
				{
					IsStackedWorkout = true,

					UserData = firstWorkout.UserData,

					Workout = new Workout()
					{
						Name = string.Join(",", workoutsToStack.Select(w => w.Workout?.Name)),
						Title = string.Join(",", workoutsToStack.Select(w => w.Workout?.Title)),
						Id = string.Join(",", workoutsToStack.Select(w => w.Workout?.Id)),

						Status = lastWorkout.Workout.Status,

						Created_At = firstWorkout.Workout.Created_At,
						Start_Time = firstWorkout.Workout.Start_Time,
						End_Time = lastWorkout.Workout.End_Time,

						Fitness_Discipline = firstWorkout.Workout.Fitness_Discipline,
						Ftp_Info = firstWorkout.Workout.Ftp_Info,
						Is_Outdoor = firstWorkout.Workout.Is_Outdoor,

						Movement_Tracker_Data = CombineMovementTrackerData(workoutsToStack),

						Total_Work = workoutsToStack.Sum(w => w.Workout?.Total_Work ?? 0),

						Ride = new Ride() // Currently only referencing Title and Instructor
						{
							Title = string.Join(",", workoutsToStack.Select(w => w.Workout?.Ride?.Title)),
							Instructor = new Instructor()
							{
								Name = string.Join(",", workoutsToStack.Select(w => w.Workout?.Ride?.Instructor?.Name))
							}
						},
					},

					WorkoutSamples = new WorkoutSamples()
					{
						Average_Summaries = new List<AverageSummary>(), // not used
						Duration = workoutsToStack.Sum(w => w.WorkoutSamples?.Duration ?? 0),
						Is_Class_Plan_Shown = false, // not used
						Has_Apple_Watch_Metrcis = firstWorkout.WorkoutSamples?.Has_Apple_Watch_Metrcis ?? false, // not used
						Is_Location_Data_Accurate = firstWorkout.WorkoutSamples?.Is_Location_Data_Accurate, // not used
						Location_Data = CombineLocationData(workoutsToStack),
						Metrics = CombineMetricsData(workoutsToStack),
						Segment_List = CombineSegments(workoutsToStack),
						Seconds_Since_Pedaling_Start = CombineSecondsArray(workoutsToStack),
						Summaries = CombineSummaries(workoutsToStack),
						Target_Performance_Metrics = CombineTargetPerformanceMetrics(workoutsToStack),
					},

					Exercises = CombineExercises(workoutsToStack),
				};

				stackedWorkouts.Add(stackedWorkout);
			} catch(Exception e)
			{
				_logger.Error(e, "Failed to build workout stack.  First workout in stack is {0}.", stack.Value.FirstOrDefault()?.Workout?.Name);
			}
		}

		return stackedWorkouts;
	}

	public static ICollection<LocationData> CombineLocationData(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedLocationData = new List<LocationData>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0) 
			return stackedLocationData;

		var totalSecondsSoFar = 0;
		foreach (var workout in workoutsToStack)
		{
			if (workout.WorkoutSamples is object
				&& workout.WorkoutSamples.Location_Data is object
				&& workout.WorkoutSamples.Location_Data.Count > 0)
			{
				var adjustedLocationData = workout.WorkoutSamples
										.Location_Data
										.Where(l => l.Coordinates is object && l.Coordinates.Count > 0)
										.Select(l =>
										{
											var adjustedCoords = l.Coordinates
												.Select(c =>
												{
													if (c is null) return c;

													c.Seconds_Offset_From_Start += totalSecondsSoFar;
													return c;
												});

											l.Coordinates = adjustedCoords.ToList();
											return l;
										});

				stackedLocationData.AddRange(adjustedLocationData);
			}
			
			totalSecondsSoFar += workout?.WorkoutSamples?.Duration ?? 0;
		}

		return stackedLocationData;
	}

	public static ICollection<Metric> CombineMetricsData(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedMetricData = new List<Metric>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return stackedMetricData;

		foreach (var workout in workoutsToStack)
		{
			if (workout.WorkoutSamples is null
				|| workout.WorkoutSamples.Metrics is null)
				continue;

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
						Zones = metric.Zones ?? new List<Zone>(),
						Missing_Data_Duration = 0,
					};
					stackedMetricData.Add(aggregateSlug);
				}

				aggregateSlug.Values = aggregateSlug.Values.Concat(metric.Values).ToArray();
				aggregateSlug.Missing_Data_Duration += metric.Missing_Data_Duration;
				aggregateSlug.Max_Value = aggregateSlug.Max_Value > metric.Max_Value ? aggregateSlug.Max_Value : metric.Max_Value;

				// zones
				if (metric.Zones is object)
				{
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
				}

				// Alternatives - Won't Support for MVP
			}
		}

		foreach (var stackedMetric in stackedMetricData)
		{
			stackedMetric.Average_Value = stackedMetric.Values.Average();
		}

		return stackedMetricData;
	}

	public static MovementTrackerData CombineMovementTrackerData(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedRepitionData = new List<RepetitionSummaryData>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return new MovementTrackerData();

		var totalSecondsSoFar = 0;
		foreach (var workout in workoutsToStack)
		{
			if (workout is null) continue;

			if (workout.Workout?.Movement_Tracker_Data is null)
			{
				totalSecondsSoFar += workout.WorkoutSamples?.Duration ?? 0;
				continue;
			}

			var repitionData = workout.Workout.Movement_Tracker_Data.Completed_Movements_Summary_Data?.Repetition_Summary_Data;

			var adjustedRepiritonData = repitionData?
										.Select(r =>
										{
											return new RepetitionSummaryData()
											{
												Movement_Id = r.Movement_Id,
												Movement_Name = r.Movement_Name,
												Tracking_Type = r.Tracking_Type,
												Completed_Reps = r. Completed_Reps,
												Completed_Duration = r.Completed_Duration,
												Offset = r.Offset + totalSecondsSoFar,
												Length = r.Length,
												Weight = r.Weight,
											};
										});

			if (adjustedRepiritonData is object)
				stackedRepitionData.AddRange(adjustedRepiritonData);
			
			totalSecondsSoFar += workout.WorkoutSamples?.Duration ?? 0;
		}

		var stackedData = new MovementTrackerData()
		{
			Completed_Movements_Summary_Data = new CompletedMovementsSummaryData()
			{
				Repetition_Summary_Data = stackedRepitionData
			}
		};

		return stackedData;
	}

	public static ICollection<Segment> CombineSegments(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedSegments = new List<Segment>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return stackedSegments;

		var totalSecondsSoFar = 0;
		foreach (var workout in workoutsToStack)
		{
			var segments = workout.WorkoutSamples?.Segment_List;

			var adjustedSegments = segments?.Select(s =>
			{
				return new Segment()
				{
					Length = s.Length,
					Start_Time_Offset = s.Start_Time_Offset + totalSecondsSoFar,
					SubSegments_V2 = s.SubSegments_V2?.Select(s2 =>
					{
						return new SubSegment()
						{
							Type = s2.Type,
							Offset = s2.Offset + totalSecondsSoFar,
							Length = s2.Length,
							Rounds = s2.Rounds,
							Movements = s2.Movements
						};
					}).ToList()
				};
			});

			if (adjustedSegments is object)
				stackedSegments.AddRange(adjustedSegments);
			
			totalSecondsSoFar += workout.WorkoutSamples?.Duration ?? 0;
		}

		return stackedSegments;
	}

	public static ICollection<int> CombineSecondsArray(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedSeconds = new List<int>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return stackedSeconds;

		var totalSecondsSoFar = 0;
		foreach (var workout in workoutsToStack)
		{
			var secondsList = workout.WorkoutSamples?.Seconds_Since_Pedaling_Start;

			var adjustedSeconds = secondsList?.Select(s =>
			{
				return s + totalSecondsSoFar;
			});

			if (adjustedSeconds is object)
				stackedSeconds.AddRange(adjustedSeconds);

			totalSecondsSoFar += workout.WorkoutSamples?.Duration  ?? 0;
		}

		return stackedSeconds;
	}

	public static ICollection<Summary> CombineSummaries(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedSummaries = new List<Summary>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return stackedSummaries;

		foreach (var workout in workoutsToStack)
		{
			if (workout.WorkoutSamples is null
				|| workout.WorkoutSamples.Summaries is null)
				continue;

			foreach (var summary in workout.WorkoutSamples.Summaries)
			{
				var aggregateSlug = stackedSummaries.FirstOrDefault(s => s.Slug == summary.Slug);

				if (aggregateSlug is null)
				{
					aggregateSlug = new Summary()
					{
						Slug = summary.Slug,
						Display_Name = summary.Display_Name,
						Display_Unit = summary.Display_Unit,
						Value = 0.0
					};
					stackedSummaries.Add(aggregateSlug);
				}

				aggregateSlug.Value = aggregateSlug.Value + summary.Value;
			}
		}

		return stackedSummaries;
	}

	public static TargetPerformanceMetrics CombineTargetPerformanceMetrics(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedTargetPerformanceMetrics = new TargetPerformanceMetrics();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return stackedTargetPerformanceMetrics;

		stackedTargetPerformanceMetrics.Cadence_Time_In_Range = workoutsToStack.Sum(w => w.WorkoutSamples?.Target_Performance_Metrics?.Cadence_Time_In_Range ?? 0);
		stackedTargetPerformanceMetrics.Resistance_Time_In_Range = workoutsToStack.Sum(w => w.WorkoutSamples?.Target_Performance_Metrics?.Resistance_Time_In_Range ?? 0);

		var stackedTargetGraphMetrics = new List<TargetGraphMetrics>();

		foreach (var workout in workoutsToStack)
		{
			if (workout.WorkoutSamples?.Target_Performance_Metrics?.Target_Graph_Metrics is null)
				continue;

			foreach (var graphMetric in workout.WorkoutSamples.Target_Performance_Metrics.Target_Graph_Metrics)
			{
				var aggregateSlug = stackedTargetGraphMetrics.FirstOrDefault(s => s.Type == graphMetric.Type);

				if (aggregateSlug is null)
				{
					aggregateSlug = new TargetGraphMetrics()
					{
						Type = graphMetric.Type,
						Max = 0, // not used
						Min = 0, // not used
						Average = 0, // not used
						Graph_Data = new GraphData()
						{
							Upper = new int[0],
							Lower = new int[0],
							Average = new int[0],
						}
					};

					stackedTargetGraphMetrics.Add(aggregateSlug);
				}

				aggregateSlug.Graph_Data.Upper = aggregateSlug.Graph_Data.Upper.Union(graphMetric.Graph_Data?.Upper ?? new int[0]).ToArray();
				aggregateSlug.Graph_Data.Lower = aggregateSlug.Graph_Data.Lower.Union(graphMetric.Graph_Data?.Lower ?? new int[0]).ToArray();
				aggregateSlug.Graph_Data.Average = aggregateSlug.Graph_Data.Average.Union(graphMetric.Graph_Data?.Average ?? new int[0]).ToArray();
			}
		}

		stackedTargetPerformanceMetrics.Target_Graph_Metrics = stackedTargetGraphMetrics;
		return stackedTargetPerformanceMetrics;
	}

	public static ICollection<P2GExercise> CombineExercises(ICollection<P2GWorkout> workoutsToStack)
	{
		var stackedExercises = new List<P2GExercise>();

		if (workoutsToStack is null || workoutsToStack.Count <= 0)
			return stackedExercises;

		var totalSecondsSoFar = 0;
		foreach (var workout in workoutsToStack)
		{
			var exercises = workout.Exercises;

			var adjustedExercises = exercises?.Select(s =>
			{
				return new P2GExercise()
				{
					Id = s.Id,
					Name = s.Name,
					StartOffsetSeconds = s.StartOffsetSeconds + totalSecondsSoFar,
					DurationSeconds = s.DurationSeconds,
					Type = s.Type,
					Reps = s.Reps,
					Weight = s.Weight,
				};
			});

			if (adjustedExercises is object)
				stackedExercises.AddRange(adjustedExercises);
			
			totalSecondsSoFar += workout.WorkoutSamples?.Duration ?? 0;
		}

		return stackedExercises;
	}
}
