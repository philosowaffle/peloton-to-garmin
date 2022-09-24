using Common.Dto.Peloton;
using System.Collections.Generic;

namespace Common.Dto.Api
{
	public class PelotonWorkoutsGetRequest : IPagingRequest
	{
		public int PageSize { get; set; }
		public int PageIndex { get; set; }
	}

	public class PelotonWorkoutsGetResponse : PagingResponseBase<Workout>
	{
		public PelotonWorkoutsGetResponse()
		{
			Items = new List<Workout>();
		}
		public override ICollection<Workout> Items { get; set; }
	}
}
