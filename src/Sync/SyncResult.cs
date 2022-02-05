using System.Collections.Generic;

namespace Sync
{
	public class SyncResult
	{
		public SyncResult()
		{
			Errors = new List<ErrorResponse>();
		}

		public bool SyncSuccess { get; set; }
		public bool PelotonDownloadSuccess { get; set; }
		public bool? ConversionSuccess { get; set; }
		public bool? UploadToGarminSuccess { get; set; }
		public ICollection<ErrorResponse> Errors { get; set; }
	}

	public class ErrorResponse
	{
		public string Message { get; set; }
	}
}