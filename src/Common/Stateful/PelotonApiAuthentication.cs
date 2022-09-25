namespace Common.Stateful
{
	public class PelotonApiAuthentication : IApiAuthentication
	{
		public string Email { get; set; }
		public string Password { get; set; }
		public string UserId { get; set; }
		public string SessionId { get; set; }

		public bool IsValid(Settings settings)
		{
			return Email == settings.Peloton.Email
				&& Password == settings.Peloton.Password
				&& !string.IsNullOrEmpty(UserId)
				&& !string.IsNullOrEmpty(SessionId);
		}
	}
}
