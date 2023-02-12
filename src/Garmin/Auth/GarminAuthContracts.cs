namespace Garmin.Auth;

public interface GarminResultWrapper
{
	string RawResponseBody { get; set; }
}

public class SendCredentialsResult : GarminResultWrapper
{
	public bool WasRedirected { get; set; }
	public string RedirectedTo { get; set; }
	public string RawResponseBody { get; set; }
}

public class SendMFAResult : GarminResultWrapper
{
	public string RawResponseBody { get; set; }
}
