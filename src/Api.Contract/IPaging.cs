using Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Api.Contract;

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
	public bool HasNext { get; }
	public bool HasPrevious { get; }

	public ICollection<T> Items { get; set; }
}

public abstract class PagingResponseBase<T> : IPagingResponse<T>
{
	public int PageIndex { get; set; }

	public int PageSize { get; set; }

	public int PageCount { get; set; }

	public int TotalItems { get; set; }

	public bool HasNext => PageIndex < PageCount;

	public bool HasPrevious => PageIndex > 0;

	public abstract ICollection<T> Items { get; set; }

}