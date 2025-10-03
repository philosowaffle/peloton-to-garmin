namespace Api.Contract;

public interface IErrorResponse
{
	public string Message { get; set; }
	public ErrorCode Code { get; set; }
}

public class ErrorResponse : IErrorResponse
{
	public string Message { get; set; }
	public ErrorCode Code { get; set; }
	public Exception? Exception { get; set; }

	public ErrorResponse() 
	{
		Message = string.Empty;
	}

	public ErrorResponse(string message, Exception? ex = null)
	{
		Message = message;
	}

	public ErrorResponse(string message, ErrorCode code, Exception? ex = null)
	{
		Message = message;
		Code = code;
	}
}

public enum ErrorCode : ushort
{
	None = 0,

	NeedToInitGarminMFAAuth = 100,
	UnexpectedGarminMFA = 101,
	InvalidGarminCredentials = 102,
}
