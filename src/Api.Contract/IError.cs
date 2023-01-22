﻿namespace Api.Contract;

public interface IErrorResponse
{
	public string Message { get; set; }
}

public class ErrorResponse : IErrorResponse
{
	public string Message { get; set; }

	public ErrorResponse(string message)
	{
		Message = message;
	}
}
