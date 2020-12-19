namespace Peloton.Dto
{
	public class WorkoutSummary
	{
		public string Id { get; set; }
		public string Workout_Id { get; set; }
		public long Instant { get; set; }
		public int Seconds_Since_Pedaling_Start { get; set; }
		public double Total_Work { get; set; }
		public double Power { get; set; }
		public double Max_Power { get; set; }
		public double Avg_Power { get; set; }
		public double Cadence { get; set; }
		public double Max_Cadence { get; set; }
		public double Avg_Cadence { get; set; }
		public double Resistance { get; set; }
		public double Max_Resistance {get; set;}
		public double Avg_Resistance { get; set; }
		public double Speed { get; set; }
		public double Max_Speed { get; set; }
		public double Avg_Speed { get; set; }
		public double Distance { get; set; }
		public double Calories { get; set; }
		public double Heart_Rate { get; set; }
		public double Max_Heart_Rate { get; set; }
		public double Avg_Heart_Rate { get; set; }
	}
}
