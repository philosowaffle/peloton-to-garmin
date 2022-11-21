using Common.Dto.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Common.Dto;

public interface IServiceResult<T>
{
	bool Successful { get; set; }
	IServiceError Error { get; set; }
	T Result { get; set; }
}

public class ServiceResult<T> : IServiceResult<T>
{
	public bool Successful { get; set; } = true;
	public IServiceError Error { get; set; }
	public T Result { get; set; }
}

public interface IServiceError
{
	Exception Exception { get; init; }
	string Message { get; init; }
	bool IsServerException { get; init; }
}

public class ServiceError : IServiceError
{
	public Exception Exception { get; init; }
	public string Message { get; init; }
	public bool IsServerException { get; init; }
}

public static class ServiceResultExtensions
{
	public static bool IsErrored<T>(this IServiceResult<T> serviceResult) => !serviceResult.Successful;

	public static ActionResult GetResultForError<T>(this IServiceResult<T> serviceResult)
	{
		if (serviceResult.Error is null)
			return new ObjectResult(new ErrorResponse($"Unexpected error occurred.")) 
			{
				StatusCode = StatusCodes.Status500InternalServerError
			};

		if (serviceResult.Error.IsServerException)
		{
			return new ObjectResult(new ErrorResponse(serviceResult.Error.Message))
			{
				StatusCode = StatusCodes.Status500InternalServerError
			};
		}

		return new BadRequestObjectResult(new ErrorResponse(serviceResult.Error.Message));
	}
}