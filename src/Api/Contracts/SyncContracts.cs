using Common.Database;

namespace Api.Contracts;

public class ErrorResponse
{
	public ErrorResponse(Sync.ErrorResponse resp)
	{
		Message = resp.Message;
	}

	public string Message { get; set; }
}

public class SyncGetResponse
{
	public bool SyncEnabled { get; set; }
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
	public Status SyncStatus { get; set; }
	public DateTime? LastSyncTime { get; set; }
	public DateTime? LastSuccessfulSyncTime { get; set; }
	public DateTime? NextSyncTime { get; set; }
}

public class SyncPostRequest
{
	public SyncPostRequest()
	{
		WorkoutIds = new List<string>();
	}

	public int NumWorkouts { get; set; }
	public ICollection<string> WorkoutIds { get; set; }
}

public class SyncPostResponse
{
	public SyncPostResponse()
	{
		Errors = new List<ErrorResponse>();
	}

	public bool SyncSuccess { get; set; }
	public bool PelotonDownloadSuccess { get; set; }
	public bool? ConverToFitSuccess { get; set; }
	public bool? UploadToGarminSuccess { get; set; }
	public ICollection<ErrorResponse> Errors { get; set; }
}
