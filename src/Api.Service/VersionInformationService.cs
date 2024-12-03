using Common;
using Common.Observe;
using Core.GitHub;
using Microsoft.Extensions.Caching.Memory;
using Philosowaffle.Capability.ReleaseChecks.Model;
using Serilog;

namespace Api.Service;

public interface IVersionInformationService
{
	Task<LatestReleaseInformation> GetLatestReleaseInformationAsync();
}

public class VersionInformationService : IVersionInformationService
{
	private static readonly ILogger _logger = LogContext.ForClass<VersionInformationService>();
	private static readonly object _lock = new object();

	private readonly IGitHubReleaseCheckService _gitHubService;
	private readonly IMemoryCache _cache;

	public VersionInformationService(IGitHubReleaseCheckService gitHubService, IMemoryCache cache)
	{
		_gitHubService = gitHubService;
		_cache = cache;
	}

	public Task<LatestReleaseInformation> GetLatestReleaseInformationAsync()
	{
		using var tracing = Tracing.Trace($"{nameof(VersionInformationService)}.{nameof(GetLatestReleaseInformationAsync)}");

		try
		{
			lock (_lock)
			{
				var key = $"LatestReleaseInformation";
				return _cache.GetOrCreateAsync(key, (cacheEntry) =>
				{
					cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

					return _gitHubService.GetLatestReleaseInformationAsync("philosowaffle", "peloton-to-garmin", Constants.AppVersion);
				});
			}
		}
		catch (Exception e)
		{
			_logger.Error("Failed to fetch Latest P2G Release information.", e);
			return Task.FromResult(new LatestReleaseInformation());
		}
	}
}
