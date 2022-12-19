namespace Common.Dto.Peloton
{
	public class AverageSummary
	{
		public string Display_Name { get; set; }
		public string Display_Units { get; set; }
		public double? Value { get; set; }
		public string Slug { get; set; } // enum
	}
}
