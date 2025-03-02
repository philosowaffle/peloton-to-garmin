using Common.Dto;
using System;
using System.Collections.Generic;
using System.Text.Json;

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

	public static void DumpAsJson(this object obj)
	{
		var serialized = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
		Console.Out.WriteLine(serialized);
	}
}
