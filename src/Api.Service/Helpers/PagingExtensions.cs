using Api.Contract;

namespace Api.Service.Helpers;

public static class PagingExtensions
{
	public static bool IsValid(this IPagingRequest request, out ErrorResponse? result)
	{
		if (request.PageSize.CheckIsLessThanOrEqualTo(0, nameof(request.PageSize), out result))
			return false;

		if (request.PageIndex.CheckIsLessThan(0, nameof(request.PageIndex), out result))
			return false;

		return true;
	}
}