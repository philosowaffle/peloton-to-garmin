namespace Common.Stateful
{
	public interface IApiAuthentication
	{
		public string Email { get; set; }
		public string Password { get; set; }
		
		bool IsValid(Settings settings); 
	}
}
