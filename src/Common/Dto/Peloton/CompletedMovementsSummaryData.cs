using System.Collections.Generic;

namespace Common.Dto.Peloton;

public record MovementTrackerData
{
	public CompletedMovementsSummaryData Completed_Movements_Summary_Data { get; init; }
}

public record CompletedMovementsSummaryData
{
	public ICollection<RepetitionSummaryData> Repetition_Summary_Data { get; init; }
}

public record RepetitionSummaryData
{
	public string Movement_Id { get; init; }
	public string Movement_Name { get; init; }
	/// <summary>
	/// When doing the exercise for time, this will be `time_tracked_rep`.
	/// </summary>
	public string Tracking_Type { get; init; } // time_tracked_rep, rep_counting, time_based
	/// <summary>
	/// Completed reps by the user (as measured by Peloton),.
	/// Only set for time_tracked_rep, rep_counting
	/// </summary>
	public int Completed_Reps { get; init; }
	/// <summary>
	/// This will only be set when the Tracking_Type is
	/// time_traced_rep or time_based.
	/// </summary>
	public int Completed_Duration { get; init; }
	/// <summary>
	/// Seconds offset from start.... I think?
	/// </summary>
	public int Offset { get; init; }
	/// <summary>
	/// Length in seconds to complete exercise
	/// </summary>
	public int Length { get; init; }
	public ICollection<Weight> Weight { get; init; }
}

public static class TrackingTypes
{
	public const string TimeTrackedRep = "time_tracked_rep";
	public const string RepCounting = "rep_counting";
	public const string TimeBased = "time_based";
	public const string Rounds = "rouds"; // AMRAP
}

public record Weight
{
	//[JsonConverter(typeof(JsonStringEnumConverter))] 
	//public WeightCategory Weight_Category { get; init; }
	public WeightData Weight_Data { get; init; }
}

public record WeightData
{
	public double Weight_Value { get; init; }

	/// <summary>
	/// lb
	/// </summary>
	public string Weight_Unit { get; init; }
}

//public enum WeightCategory : byte
//{
//	Light = 1,
//	Medium = 2,
//	Heavy = 3,
//	Body_Weight = 4,
//}