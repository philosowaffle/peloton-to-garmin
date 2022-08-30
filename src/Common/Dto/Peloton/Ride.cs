using System;
using System.Collections.Generic;

namespace Common.Dto.Peloton
{
	public class Ride
	{
		// instructor object
		public ICollection<string> Class_Type_Ids { get; set; }
		public string Content_Provider { get; set; }
		public string Content_Format { get; set; }
		public string Description { get; set; }
		public double Difficulty_Estimate { get; set; }
		public double Overall_Estimate { get; set; }
		public double Difficulty_Rating_Avg { get; set; }
		public int Difficulty_Rating_Count { get; set; }
		// difficutly level
		public int? Duration { get; set; }
		public ICollection<string> Equipment_Ids { get; set; }
		// equip tags
		// extra images
		public string Fitness_Discipline { get; set; } //enum
		public string Fitness_Discipline_Display_Name { get; set; }
		// closed captions
		public bool Has_Pedaling_Metrics { get; set; }
		// peloton id
		public string Id { get; set; }
		public Uri Image_Url { get; set; }
		public string Instructor_Id { get; set; }
		public bool Is_Archived { get; set; }
		// closed caption shown
		// explicit
		// has free mode
		// live studio only
		// language
		// orgin_locale
		public int Length { get; set; }
		// live stream id
		// live stream url
		// location
		public ICollection<string> Metrics { get; set; } // enum
														 // original air time
														 // overall rating avg
														 // overall rating count
		public int Pedaling_Start_Offset { get; set; }
		public int Pedaling_End_Offset { get; set; }
		public int Pedaling_Duration { get; set; }
		// rating
		// ride type id
		// ride type ids
		public string Title { get; set; }
		public Instructor Instructor { get; set; }

	}

	public class Instructor
	{
		public string Name { get; set; }
	}
}
