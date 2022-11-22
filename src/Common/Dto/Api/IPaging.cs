using Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Common.Dto.Api;

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

public static class PagingExtensions
{
	public static bool IsValid(this IPagingRequest request, out ActionResult result)
	{
		if (request.PageSize.CheckIsLessThanOrEqualTo(0, nameof(request.PageSize), out result))
			return false;

		if (request.PageIndex.CheckIsLessThan(0, nameof(request.PageIndex), out result))
			return false;

		return true;
	}
}

