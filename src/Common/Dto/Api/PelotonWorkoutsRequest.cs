using Common.Dto.Peloton;
using System;
using System.Collections.Generic;

namespace Common.Dto.Api;

public class PelotonWorkoutsGetRequest : IPagingRequest
{
	public int PageSize { get; set; }
	public int PageIndex { get; set; }
}

public class PelotonWorkoutsGetResponse : PagingResponseBase<PelotonWorkout>
{
	public PelotonWorkoutsGetResponse()
	{
		Items = new List<PelotonWorkout>();
	}

	public override ICollection<PelotonWorkout> Items { get; set; }
}

public record PelotonWorkoutsGetAllRequest
{
	/// <summary>
	/// Load workout from this date till now. UTC
	/// </summary>
	public DateTime SinceDate { get; set; }
	/// <summary>
	/// Exclude these WorkoutTypes from the results.
	/// Default returns all.
	/// </summary>
	public ICollection<WorkoutType> ExcludeWorkoutTypes { get; init; } = new List<WorkoutType>(0);
	/// <summary>
	/// Only include workouts with this Status. Bitmask.
	/// Default returns all.
	/// </summary>
	public WorkoutStatus? WorkoutStatusFilter { get; init; }
}

public record PelotonWorkoutsGetAllResponse
{
	public DateTime SinceDate { get; init; }
	public ICollection<PelotonWorkout> Items { get; init; } = new List<PelotonWorkout>();

}

public record PelotonWorkout
{
	public PelotonWorkout() { }

	public PelotonWorkout(Workout workout)
	{
		Id = workout.Id;
		Status = workout.Status;
		ClassTypeTitle = workout.Title;
		WorkoutTitle = workout.Ride?.Title;
		Name = workout.Name;
		Created_At = workout.Created_At;
	}

	public string Id { get; init; }
	public string Status { get; init; }
	public string ClassTypeTitle { get; init; }
	public string WorkoutTitle { get; init; }
	public string Name { get; init; }
	public long Created_At { get; init; }
}

[Flags]
public enum WorkoutStatus : byte
{
	Completed = 1,
}