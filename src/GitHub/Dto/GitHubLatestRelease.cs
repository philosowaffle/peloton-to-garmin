namespace GitHub.Dto;

public class GitHubLatestRelease
{
	public string? Html_Url { get; set; }
	public string? Tag_Name { get; set; }
	public DateTime Published_At { get; set; }
	public string? Body { get; set; }
}
