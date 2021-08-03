using Common;
using Serilog;
using System;

namespace Garmin
{
	public class GarminUploadException : Exception
	{
		private static readonly ILogger _logger = LogContext.ForClass<GarminUploadException>();

		private int ExitCode;

		public GarminUploadException(string message, int exitCode) : base($"GUpload Exit Code: {exitCode} - {message}")
		{
			ExitCode = exitCode;
			_logger.Error("Failed to upload workouts. GUpload had exit code of {ExitCode}", ExitCode);
		}

		public GarminUploadException(string message, int exitCode, Exception innerException) : base($"GUpload Exit Code: {exitCode} - {message}", innerException)
		{
			ExitCode = exitCode;
			_logger.Error("Failed to upload workouts. GUpload had exit code of {ExitCode}", ExitCode);
		}
	}
}
