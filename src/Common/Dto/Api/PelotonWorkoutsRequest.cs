using Common.Dto.Peloton;
using System.Collections.Generic;

namespace Common.Dto.Api
{
	public class PelotonWorkoutsGetRequest : IPagingRequest
	{
		public int PageSize { get; set; }
		public int PageIndex { get; set; }
	}

	public class PelotonWorkoutsGetResponse : PagingResponseBase<RecentWorkout>
	{
		public PelotonWorkoutsGetResponse()
		{
			Items = new List<RecentWorkout>();
		}
		public override ICollection<RecentWorkout> Items { get; set; }
	}
}
