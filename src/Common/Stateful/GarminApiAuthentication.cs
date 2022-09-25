using Flurl.Http;

namespace Common.Stateful
{
	public class GarminApiAuthentication : IApiAuthentication
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public CookieJar CookieJar { get; set; }

		public bool IsValid(Settings settings)
		{
			return Email == settings.Garmin.Email
				&& Password == settings.Garmin.Password
				&& CookieJar is object;
		}
	}
}
