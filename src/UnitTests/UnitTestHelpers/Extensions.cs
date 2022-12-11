using Common.Dto;
using System.Collections;
using System.Collections.Generic;

namespace UnitTests.UnitTestHelpers;

public static class Extensions
{
	public static ServiceResult<ICollection<T>> AsServiceResult<T>(this ICollection<T> obj)
	{
		return new ServiceResult<ICollection<T>>()
		{
			Result = obj
		};
	}
}
