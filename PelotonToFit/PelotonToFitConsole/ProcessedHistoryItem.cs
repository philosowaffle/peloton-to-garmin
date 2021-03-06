using System;

namespace PelotonToFitConsole
{
	public class ProcessedHistoryItem
	{
		public string WorkoutTitle { get; set; }
		public DateTime ProcessedDate { get; set; }
		public DateTime WorkoutDate { get; set; }
		public string WorkoutId { get; set; }
	}
}
