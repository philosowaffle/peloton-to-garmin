using GitHub.Dto;
using Flurl.Http;

namespace GitHub;

public interface IGitHubApiClient
{
	Task<GitHubLatestRelease> GetLatestReleaseAsync();
}

public class ApiClient : IGitHubApiClient
{
	private const string BASE_URL = "https://api.github.com";
	private const string USER = "philosowaffle";
	private const string REPO = "peloton-to-garmin";

	public Task<GitHubLatestRelease> GetLatestReleaseAsync()
	{
		return $"{BASE_URL}/repos/{USER}/{REPO}/releases/latest"
			.WithHeader("Accept", "application/json")
			.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:105.0) Gecko/20100101 Firefox/105.0")
		.GetJsonAsync<GitHubLatestRelease>();
	}
}
