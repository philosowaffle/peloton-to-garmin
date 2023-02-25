using System.Collections.Generic;
using System.Text.Json.Serialization;

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
	/// True when doing the exercise for Time instead of for Reps
	/// </summary>
	public bool Is_Hold { get; init; }
	public int Target_Number { get; init; }
	public int Completed_Number { get; init; }
	/// <summary>
	/// Seconds offset from start.... I think?
	/// </summary>
	public int Offset { get; init; }
	/// <summary>
	/// Length in seconds to complete exercise
	/// </summary>
	public int Length { get; init; }
	/// <summary>
	/// Total load of the weight lifted and number of reps
	/// </summary>
	public int? Total_Volume { get; init; }
	public ICollection<Weight> Weight { get; init; }
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