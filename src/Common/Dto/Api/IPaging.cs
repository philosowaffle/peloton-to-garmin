using System.Collections.Generic;

namespace Common.Dto.Api
{
	public interface IPagingRequest
	{
		public int PageIndex { get; set; }
		public int PageSize { get; set; }
	}

	public interface IPagingResponse<T>
	{
		public int PageIndex { get; set; }
		public int PageSize { get; set; }
		public int PageCount { get; set; }
		public int TotalItems { get; set; }
		public bool HasNext { get; set; }
		public bool HasPrevious { get; set; }

		public ICollection<T> Items { get; set; }
	}
}
