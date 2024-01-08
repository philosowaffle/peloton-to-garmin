using Api.Contract;
using Common.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Service.Helpers;

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