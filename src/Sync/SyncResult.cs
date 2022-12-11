using Common.Dto;
using System;
using System.Collections.Generic;

namespace Sync;

public class SyncResult
{
	public SyncResult()
	{
		Errors = new List<IServiceError>();
	}

	public bool SyncSuccess { get; set; }
	public bool PelotonDownloadSuccess { get; set; }
	public bool? ConversionSuccess { get; set; }
	public bool? UploadToGarminSuccess { get; set; }
	public ICollection<IServiceError> Errors { get; set; }
}

public class ErrorResponse : IServiceError
{
	public ErrorResponse()
	{
		Message = string.Empty;
	}

	public Exception? Exception { get; init; }
	public string Message { get; init; }
	public bool IsServerException { get; init; }
}