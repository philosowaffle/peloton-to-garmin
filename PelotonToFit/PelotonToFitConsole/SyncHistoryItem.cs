using System;

namespace PelotonToFitConsole
{
	public class SyncHistoryItem
	{
		public string WorkoutId { get; set; }
		public string WorkoutTitle { get; set; }
		public DateTime DownloadDate { get; set; }
		public DateTime WorkoutDate { get; set; }
		public DateTime? GarminUploadDate { get; set; }
		public bool ConvertedToFit { get; set; }
	}
}