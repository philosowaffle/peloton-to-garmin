using Common.Dto.Api;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Helpers;

public static class Guard
{
	public static bool CheckIsNull<T>(this T input, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input is null)
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must not be null."));
			return true;
		}

		return false;
	}

	public static bool CheckIsNotNull<T>(this T input, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input is not null)
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must be null."));
			return true;
		}

		return false;
	}

	public static bool CheckIsGreaterThan(this int input, int limit, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input > limit)
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must not be greater than {limit}."));
			return true;
		}

		return false;
	}

	public static bool CheckIsLessThanOrEqualTo(this int input, int limit, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input <= limit)
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must be greater than {limit}."));
			return true;
		}

		return false;
	}

	public static bool CheckIsLessThan(this int input, int limit, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input < limit)
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must be greater than or equal to {limit}."));
			return true;
		}

		return false;
	}

	public static bool CheckHasAny<T>(this ICollection<T> input, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input is not null && input.Any())
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must be empty."));
			return true;
		}

		return false;
	}

	public static bool CheckDoesNotHaveAny<T>(this ICollection<T> input, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input is null || !input.Any())
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must not be empty."));
			return true;
		}

		return false;
	}

	public static bool IsAfter(this DateTime input, DateTime limit, string name, out ActionResult result, string errorMessage = null)
	{
		result = null;

		if (input > limit)
		{
			result = new BadRequestObjectResult(new ErrorResponse(errorMessage ?? $"{name} must be before {limit}."));
			return true;
		}

		return false;
	}
}
