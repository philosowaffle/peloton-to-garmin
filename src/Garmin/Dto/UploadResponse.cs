using System.Collections.Generic;

namespace Garmin.Dto
{
	public class UploadResponse
	{
		public DetailedImportResult DetailedImportResult { get; set; }
	}

	public class DetailedImportResult
	{
		public string CreationDate { get; set; }
		public string FileName { get; set; }
		public int? FileSize { get; set; }
		public string IpAddress { get; set; }
		public int? ProcessingTime { get; set; }

		/// <summary>
		/// On Error, the type is string, empty string is returned.
		/// On Success, the type is int, a number is returned.
		/// </summary>
		//public int UploadId { get; set; }

		public ICollection<Failure> Failures { get; set; }
		public ICollection<Success> Successes { get; set; }
	}

	public class Success
	{
		public string ExternalId { get; set; }
		//public int? InternalId { get; set; }
		public ICollection<Messages> Messages { get; set; }
	}

	public class Failure
	{
		public string ExternalId { get; set; }
		//public int? InternalId { get; set; }
		public ICollection<Messages> Messages { get; set; }
	}

	public class Messages
	{
		public int Code { get; set; }
		public string Content { get; set; }
	}
}
