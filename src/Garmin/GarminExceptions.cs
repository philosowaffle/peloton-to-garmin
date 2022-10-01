using System;

namespace Garmin
{
	public class GarminUploadException : Exception
	{
		private int ExitCode;

		public GarminUploadException(string message, int exitCode) : base($"GUpload Exit Code: {exitCode} - {message}")
		{
			ExitCode = exitCode;
		}

		public GarminUploadException(string message, int exitCode, Exception innerException) : base($"GUpload Exit Code: {exitCode} - {message}", innerException)
		{
			ExitCode = exitCode;
		}
	}
}
