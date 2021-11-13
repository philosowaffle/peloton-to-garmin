using Common.Database;
using System.Collections.Generic;

namespace WebApp.Models
{
	public class HomeViewModel : SyncGetResponse
	{
		public ICollection<SyncHistoryItem> RecentWorkouts { get; set; }
	}
}
