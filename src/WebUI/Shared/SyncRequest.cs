using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebUI.Shared
{
	public class SyncPostRequest
	{
		[Required]
		[Range(1, 100000, ErrorMessage = "Valid range is (1-100000).")]
		public int NumWorkouts { get; set; }
	}

	public class SyncPostResponse
	{
		public SyncPostResponse()
		{
			Errors = new List<ErrorResponse>();
		}

		public bool SyncSuccess { get; set; }
		public bool PelotonDownloadSuccess { get; set; }
		public bool? ConverToFitSuccess { get; set; }
		public bool? UploadToGarminSuccess { get; set; }
		public ICollection<ErrorResponse> Errors { get; set; }
	}

	public class SyncViewModel
	{
		public SyncPostRequest Request { get; set; }
		public SyncPostResponse Response { get; set; }
	}
}
