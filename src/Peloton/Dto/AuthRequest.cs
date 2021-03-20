
namespace Peloton.Dto
{
	public class AuthRequest
	{
		public string username_or_email { get; set; }
		public string password { get; set; }
	}

	public class AuthResponse
	{
		public string user_id { get; set; }
		public string session_id { get; set; }
	}
}
