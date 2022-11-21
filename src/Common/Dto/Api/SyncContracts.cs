using Common.Database;
using System;
using System.Collections.Generic;

namespace Common.Dto.Api;

public record SyncGetResponse
{
	public bool SyncEnabled { get; init; }
	public string AutoSyncHealthString
	{
		get {
			switch (SyncStatus)
			{
				case Status.Dead: return "Dead";
				case Status.NotRunning: return "Not Running";
				case Status.Running: return "Running";
				case Status.UnHealthy: return "Unhealthy";
			}

			return "Unknown";
		}
	}
	public Status SyncStatus { get; init; }
	public DateTime? LastSyncTime { get; init; }
	public DateTime? LastSuccessfulSyncTime { get; init; }
	public DateTime? NextSyncTime { get; init; }
}

public record SyncPostRequest
{
	public SyncPostRequest()
	{
		WorkoutIds = new List<string>();
	}

	/// <summary>
	/// Sync N number of most recent workouts.
	/// 
	/// Mutually exclusive with 
	/// - WorkoutIds 
	/// - SinceDate
	/// </summary>
	public int NumWorkouts { get; init; }

	/// <summary>
	/// Sync all workouts since a certain UTC date.
	/// 
	/// Mutually exclusive with
	/// - NumWorkouts
	/// - WorkoutIds
	/// </summary>
	public DateTime? SinceDate { get; init; }

	/// <summary>
	/// Sync a specific set of workouts by their Ids.
	/// 
	/// Mutually exclusive with 
	/// - NumWorkouts
	/// - SinceDate
	/// </summary>
	public ICollection<string> WorkoutIds { get; init; }

	public bool FilterOutExcludedWorkoutTypes { get; init; }

}

public record SyncPostResponse
{
	public SyncPostResponse()
	{
		Errors = new List<ErrorResponse>();
	}

	public bool SyncSuccess { get; init; }
	public bool PelotonDownloadSuccess { get; init; }
	public bool? ConverToFitSuccess { get; init; }
	public bool? UploadToGarminSuccess { get; init; }
	public ICollection<ErrorResponse> Errors { get; init; }
}
