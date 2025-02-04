namespace Common.Dto.Peloton
{
	public class Summary
	{
		public string Display_Name { get; set; }
		public string Display_Unit { get; set; }
		public double? Value { get; set; }
		public string Slug { get; set; } // enum: total_output, distance, elevation, calories
	}
}
