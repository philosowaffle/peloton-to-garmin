namespace Common.Dto.Api
{
	public interface IErrorResponse
	{
		public string Message { get; set; }
		public ErrorCode Code { get; set; }
	}

	public class ErrorResponse : IErrorResponse
	{
		public string Message { get; set; }
		public ErrorCode Code { get; set; }

		public ErrorResponse() { }

		public ErrorResponse(string message)
		{
			Message = message;
		}

		public ErrorResponse(string message, ErrorCode code)
		{
			Message = message;
			Code = code;
		}
	}

	public enum ErrorCode : ushort
	{
		None = 0,

		NeedToInitGarminMFAAuth = 100,
	}
}
