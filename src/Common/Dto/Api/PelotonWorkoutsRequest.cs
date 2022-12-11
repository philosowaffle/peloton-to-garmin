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
		CreatedAt = workout.Created_At;
		ImageUrl = workout.Ride?.Image_Url;
	}

	public string Id { get; init; }
	public string Status { get; init; }
	public string ClassTypeTitle { get; init; }
	public string WorkoutTitle { get; init; }
	public string Name { get; init; }
	public long CreatedAt { get; init; }
	public Uri ImageUrl { get; set; }

}

[Flags]
public enum WorkoutStatus : byte
{
	Completed = 1,
}