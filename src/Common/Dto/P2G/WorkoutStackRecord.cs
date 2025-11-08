using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Dto.P2G;

public record WorkoutStackRecord
{
	public string Id { get; init; } = string.Empty;
	public ICollection<StackedWorkout> Workouts { get; init; } = new List<StackedWorkout>();
	public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
	/// <summary>
	/// We do not want to keep indefinite sync history, nor should P2G need
	/// to at this time. We will default to 1year from the sync date.
	/// This can be overriden by the user in the Settings.
	/// </summary>
	public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddYears(1);
}

public record StackedWorkout
{
	public string Id { get; init; } = string.Empty;
	public string Title { get; init; } = string.Empty;
	public string ClassId { get; init; } = string.Empty;
}

public static class WorkoutStackRecordExtensions
{
	public static bool TryFindWorkoutStackThisWorkoutBelongsTo(this ICollection<WorkoutStackRecord> previouslySeenStacks, P2GWorkout workout, out WorkoutStackRecord stackThisWorkoutBelongsTo)
	{
		stackThisWorkoutBelongsTo = null;
		if (previouslySeenStacks == null || workout is null) return false;

		stackThisWorkoutBelongsTo = previouslySeenStacks.FirstOrDefault(r => r.Workouts.Any(w => w.Id == workout.Workout.Id));
		return stackThisWorkoutBelongsTo is object;
	}

	public static bool TryFindMatchingWorkoutStack(this ICollection<WorkoutStackRecord> previouslySeenStacks, P2GWorkout workoutStack, out WorkoutStackRecord matchingStack)
	{
		matchingStack = null;
		if (previouslySeenStacks == null || workoutStack is null) return false;
		var workoutStackIds = workoutStack.StackedWorkouts.Select(w => w.Workout.Id).ToHashSet();
		matchingStack = previouslySeenStacks.FirstOrDefault(r => r.Workouts.Select(w => w.Id).ToHashSet().SetEquals(workoutStackIds));
		return matchingStack is object;
	}

	public static bool IsStackComplete(this P2GWorkout potentialStack, ICollection<WorkoutStackRecord> previouslySeenStacks)
	{
		if (potentialStack is null) return false;
		if (!potentialStack.IsStackedWorkout) return true;

		if (previouslySeenStacks.TryFindWorkoutStackThisWorkoutBelongsTo(potentialStack, out var existingStack))
		{
			return potentialStack.StackedWorkouts.Count == existingStack.Workouts.Count &&
					potentialStack.StackedWorkouts.All(newWorkout => existingStack.Workouts.Any(w => w.Id == newWorkout.Workout.Id));
		}

		return true; // this is likely a net-new stack
	}

	public static ICollection<WorkoutStackRecord> CreateNewStacksIfNeeded(this IEnumerable<P2GWorkout> potentialNewWorkoutStacks, ICollection<WorkoutStackRecord> previouslySeenStacks)
	{
		if (potentialNewWorkoutStacks is null || potentialNewWorkoutStacks.Count() == 0) return new List<WorkoutStackRecord>(0);

		var newlyCreatedStacks = new List<WorkoutStackRecord>();
		foreach (var potentialNewStack in potentialNewWorkoutStacks)
		{
			if (!potentialNewStack.IsStackedWorkout) continue;

			if (previouslySeenStacks.TryFindMatchingWorkoutStack(potentialNewStack, out var existingStack))
				continue;

			// this is a new stack, so we need to create a new record for it
			var newRecord = new WorkoutStackRecord
			{
				Id = string.Join('_', potentialNewStack.StackedWorkouts.Select(w => w.Workout.Id)),
				Workouts = potentialNewStack.StackedWorkouts.Select(w => 
				new StackedWorkout
				{
					Id = w.Workout.Id,
					Title = w.Workout.Title,
					ClassId = w.Workout.Ride?.Id,
				}).ToList(),
				CreatedAt = DateTime.UtcNow,
			};
			newlyCreatedStacks.Add(newRecord);
		}

		return newlyCreatedStacks;
	}
}