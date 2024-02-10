using System;

namespace Sync.Dto
{
	public class SyncServiceStatus
	{
		public DateTime? LastSyncTime { get; set; }
		public DateTime? NextSyncTime { get; set; }
		public DateTime? LastSuccessfulSyncTime { get; set; }
		public Status SyncStatus { get; set; }
		public string LastErrorMessage { get; set; } = string.Empty;
	}

	public enum Status : byte
	{
		NotRunning = 0,
		Running = 1,
		UnHealthy = 2,
		Dead = 3
	}
}