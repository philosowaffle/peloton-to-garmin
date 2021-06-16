using System.ComponentModel.DataAnnotations;

namespace WebUI.Shared
{
	public class SyncPostRequest
	{
		[Required]
		[Range(1, 100000, ErrorMessage = "Valid range is (1-100000).")]
		public int NumWorkouts { get; set; }
	}
}
