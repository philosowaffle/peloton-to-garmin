using System.ComponentModel.DataAnnotations;

namespace Api.Contract;

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

public enum Status : byte
{
	NotRunning = 0,
	Running = 1,
	UnHealthy = 2,
	Dead = 3
}
public record SyncPostRequest
{
	public SyncPostRequest()
	{
		WorkoutIds = new List<string>();
	}

	/// <summary>
	/// Sync a specific set of workouts by their Ids.
	/// </summary>
	public ICollection<string> WorkoutIds { get; init; }

	/// <summary>
	/// True if these workouts should be combined (stacked) into a single final workout.
	/// When False, the sync will still honor any settings configured in StackedClassesSettings.
	/// When True, the sync will ignore the StackedClassesSettings and attempt stack all classes of the same
	/// type, regardless of the time gap between them.
	/// </summary>
	public bool ForceStackWorkouts { get; init; } = false;
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

public record SyncRecentPostRequest
{
	/// <summary>
	/// Syncs the most recent NumberToSync workouts.
	/// 
	/// Mutually exclusive with 
	/// - NumWorkouts
	/// - SinceDate
	/// </summary>
	[Required]
	public int NumberToSync { get; init; }

	/// <summary>
	/// True if these workouts should be combined (stacked) into a single final workout.
	/// When False, the sync will still honor any settings configured in StackedClassesSettings.
	/// When True, the sync will ignore the StackedClassesSettings and attempt stack all classes of the same
	/// type, regardless of the time gap between them.
	/// </summary>
	public bool ForceStackWorkouts { get; init; } = false;
}

public record SyncRecentPostResponse
{
	public SyncRecentPostResponse()
	{
		Errors = new List<ErrorResponse>();
	}

	public bool SyncSuccess { get; init; }
	public bool PelotonDownloadSuccess { get; init; }
	public bool? ConverToFitSuccess { get; init; }
	public bool? UploadToGarminSuccess { get; init; }
	public ICollection<ErrorResponse> Errors { get; init; }
}