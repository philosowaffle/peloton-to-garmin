using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

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