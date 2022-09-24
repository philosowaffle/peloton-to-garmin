namespace Conversion
{
	public class ConvertStatus
	{
		public ConversionResult Result { get; set; }
		public string ErrorMessage { get; set; }
	}

	public enum ConversionResult
	{
		Success = 0,
		Skipped = 10,
		Failed = 20,	
	}
}
