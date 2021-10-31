using System;

namespace Common.Database
{
	public class SyncTime
	{
		public DateTime? LastSyncTime { get; set; }
		public DateTime? NextSyncTime { get; set; }
		public DateTime? LastSuccessfulSyncTime { get; set; }
		public string AutoSyncServiceStatus { get; set; }
	}
}