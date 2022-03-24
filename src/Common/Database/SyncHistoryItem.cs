using Common.Dto.Peloton;
using Common.Helpers;
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
		public bool ConvertedToTcx { get; set; }
		public DateTime? UploadDate { get; set; }

		public SyncHistoryItem() { }

		public SyncHistoryItem(Workout workout)
		{
			var startTimeInSeconds = workout.Start_Time;
			var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(startTimeInSeconds).ToLocalTime();

			Id = workout.Id;
			WorkoutTitle = WorkoutHelper.GetTitle(workout);
			WorkoutDate = dtDateTime;
		}
	}
}