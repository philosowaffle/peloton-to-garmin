using Common.Dto;
using System.Collections.Generic;

namespace Sync;

public class SyncResult
{
	public SyncResult()
	{
		Errors = new List<ServiceError>();
	}

	public bool SyncSuccess { get; set; }
	public bool PelotonDownloadSuccess { get; set; }
	public bool? ConversionSuccess { get; set; }
	public bool? UploadToGarminSuccess { get; set; }
	public ICollection<ServiceError> Errors { get; set; }
}