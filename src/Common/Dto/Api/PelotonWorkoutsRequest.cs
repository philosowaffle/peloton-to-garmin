using Common.Dto.Peloton;
using System;
using System.Collections.Generic;

namespace Common.Dto.Api;

public class PelotonWorkoutsGetRequest : IPagingRequest
{
	public int PageSize { get; set; }
	public int PageIndex { get; set; }
}

public class PelotonWorkoutsGetResponse : PagingResponseBase<Workout>
{
	public PelotonWorkoutsGetResponse()
	{
		Items = new List<Workout>();
	}

	public override ICollection<Workout> Items { get; set; }
}

public record PelotoWorkoutsSinceGetRequest
{
	/// <summary>
	/// Since Date UTC
	/// </summary>
	public DateTime SinceDate { get; set; }
	public bool FilterOutExcludedWorkoutTypes { get; init; }
	public bool CompletedOnly { get; init; }
}

public record PelotonWorkoutsSinceGetResponse
{
	public DateTime SinceDate { get; init; }
	public ICollection<Workout> Items { get; init; }

}