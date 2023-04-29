using Api.Contract;

namespace Api.Service.Helpers;

public static class Guard
{
	public static bool IsNull<T>(this T input, string name, out ErrorResponse? result, string? errorMessage = null)
	{
		result = null;

		if (input is null)
		{
			result = new ErrorResponse(errorMessage ?? $"{name} must not be null.");
			return true;
		}

		return false;
	}

	public static bool CheckIsNullOrEmpty(this string input, string name, out ErrorResponse? result, string? errorMessage = null)
	{
		result = null;

		if (string.IsNullOrEmpty(input))
		{
			result = new ErrorResponse(errorMessage ?? $"{name} must not be null or empty.");
			return true;
		}

		return false;
	}

	public static bool CheckIsLessThanOrEqualTo(this int input, int limit, string name, out ErrorResponse? result, string? errorMessage = null)
	{
		result = null;

		if (input <= limit)
		{
			result = new ErrorResponse(errorMessage ?? $"{name} must be greater than {limit}.");
			return true;
		}

		return false;
	}

	public static bool CheckIsLessThan(this int input, int limit, string name, out ErrorResponse? result, string? errorMessage = null)
	{
		result = null;

		if (input < limit)
		{
			result = new ErrorResponse(errorMessage ?? $"{name} must be greater than or equal to {limit}.");
			return true;
		}

		return false;
	}

	public static bool DoesNotHaveAny<T>(this ICollection<T> input, string name, out ErrorResponse? result, string? errorMessage = null)
	{
		result = null;

		if (input is null || !input.Any())
		{
			result = new ErrorResponse(errorMessage ?? $"{name} must not be empty.");
			return true;
		}

		return false;
	}

	public static bool IsAfter(this DateTime input, DateTime limit, string name, out ErrorResponse? result, string? errorMessage = null)
	{
		result = null;

		if (input > limit)
		{
			result = new ErrorResponse(errorMessage ?? $"{name} must be before {limit}.");
			return true;
		}

		return false;
	}
}
