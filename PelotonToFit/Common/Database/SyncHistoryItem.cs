using System;

namespace Common.Database
{
	public class SyncHistoryItem
	{
		public string Id { get; set; }
		public string WorkoutTitle { get; set; }
		public DateTime DownloadDate { get; set; }
		public DateTime WorkoutDate { get; set; }
		public bool ConvertedToFit { get; set; }
	}
}