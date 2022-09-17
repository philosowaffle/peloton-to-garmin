using Common.Dto.Peloton;
using System.Collections.Generic;

namespace Common.Dto.Api
{
	public class PelotonWorkoutsGetRequest : IPagingRequest
	{
		public int PageSize { get; set; }
		public int PageIndex { get; set; }
	}

	public class PelotonWorkoutsGetResponse : IPagingResponse<RecentWorkout>
	{
		public PelotonWorkoutsGetResponse()
		{
			Items = new List<RecentWorkout>();
		}

		public int PageIndex { get; set; }
		public int PageSize { get; set; }
		public int PageCount { get; set; }
		public int TotalItems { get; set; }
		public bool HasNext { get; set; }
		public bool HasPrevious { get; set; }
		public ICollection<RecentWorkout> Items { get; set; }
	}
}
