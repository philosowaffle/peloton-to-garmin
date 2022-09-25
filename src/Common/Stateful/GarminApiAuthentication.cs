using Flurl.Http;

namespace Common.Stateful
{
	public class GarminApiAuthentication
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public CookieJar CookieJar { get; set; }
	}
}
