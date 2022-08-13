using System;

namespace Common.Database
{
	public class SyncServiceStatus
	{
		public DateTime? LastSyncTime { get; set; }
		public DateTime? NextSyncTime { get; set; }
		public DateTime? LastSuccessfulSyncTime { get; set; }
		public Status SyncStatus { get; set; }
		public string LastErrorMessage { get; set; }
	}

	public enum Status
	{
		NotRunning = 0,
		Running = 1,
		UnHealthy = 2,
		Dead = 3
	}
}