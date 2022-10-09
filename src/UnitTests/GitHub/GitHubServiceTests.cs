using Common;
using FluentAssertions;
using GitHub;
using GitHub.Dto;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace UnitTests.GitHub;

public class GitHubServiceTests
{
	[Test]
	public async Task GetLatestReleaseAsync_MapsData_Correctly()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var cache = new MemoryCache(new MemoryCacheOptions());
		autoMocker.Use<IMemoryCache>(cache);

		var service = autoMocker.CreateInstance<GitHubService>();
		var api = autoMocker.GetMock<IGitHubApiClient>();		

		var gitHubResponse = new GitHubLatestRelease()
		{
			Tag_Name = "v2.3.4",
			Body = "Release notes",
			Html_Url = "https://www.google.com",
			Published_At = DateTime.Now
		};

		api.Setup(x => x.GetLatestReleaseAsync())
			.ReturnsAsync(gitHubResponse)
			.Verifiable();

		// ACT
		var result = await service.GetLatestReleaseAsync();

		// ASSERT
		result.LatestVersion.Should().Be(gitHubResponse.Tag_Name);
		result.ReleaseDate.Should().Be(gitHubResponse.Published_At);
		result.ReleaseUrl.Should().Be(gitHubResponse.Html_Url);
		result.Description.Should().Be(gitHubResponse.Body);
		result.IsReleaseNewerThanInstalledVersion.Should().BeFalse();

		api.Verify();
	}

	[Test]
	public async Task GetLatestReleaseAsync_ReturnsDefault_When_ExceptionThrown()
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var cache = new MemoryCache(new MemoryCacheOptions());
		autoMocker.Use<IMemoryCache>(cache);

		var service = autoMocker.CreateInstance<GitHubService>();
		var api = autoMocker.GetMock<IGitHubApiClient>();

		api.Setup(x => x.GetLatestReleaseAsync())
			.ThrowsAsync(new Exception())
			.Verifiable();

		// ACT
		var result = await service.GetLatestReleaseAsync();

		// ASSERT
		result.LatestVersion.Should().BeNull();
		result.ReleaseDate.Should().Be(DateTime.MinValue);
		result.ReleaseUrl.Should().BeNull();
		result.Description.Should().BeNull();
		result.IsReleaseNewerThanInstalledVersion.Should().BeFalse();

		api.Verify();
	}

	[TestCase(null , ExpectedResult = false)]
	[TestCase("", ExpectedResult = false)]
	[TestCase("abacva", ExpectedResult = false)]
	[TestCase("0.0.1" , ExpectedResult = false)]
	[TestCase("v0.0.1" , ExpectedResult = false)]
	[TestCase("0.1.1", ExpectedResult = false)]
	[TestCase("1.1.1", ExpectedResult = false)]
	[TestCase(Constants.AppVersion, ExpectedResult = false)]
	[TestCase("8.0.0", ExpectedResult = true)]
	[TestCase("8.0.1", ExpectedResult = true)]
	public async Task<bool> GetLatestReleaseAsync_Calculates_IsNewVersion_Correctly(string ghVersion)
	{
		// SETUP
		var autoMocker = new AutoMocker();
		var cache = new MemoryCache(new MemoryCacheOptions());
		autoMocker.Use<IMemoryCache>(cache);

		var service = autoMocker.CreateInstance<GitHubService>();
		var api = autoMocker.GetMock<IGitHubApiClient>();

		var gitHubResponse = new GitHubLatestRelease()
		{
			Tag_Name = ghVersion,
		};

		api.Setup(x => x.GetLatestReleaseAsync())
			.ReturnsAsync(gitHubResponse)
			.Verifiable();

		// ACT
		var result = await service.GetLatestReleaseAsync();

		// ASSERT
		api.Verify();
		return result.IsReleaseNewerThanInstalledVersion;
	}
}
