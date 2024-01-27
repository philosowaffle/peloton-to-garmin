using Common;
using Flurl.Http;
using System;

namespace Garmin.Dto;

public record P2GGarminData
{
	public EncryptionVersion EncryptionVersion { get; set; }
	public string OAuth1Token { get; set; }
	public string OAuth2Token { get; set; }
	public StagedPartialGarminAuthentication PartialGarminAuthentication { get; set; }

}

public record StagedPartialGarminAuthentication
{
	public DateTime ExpiresAt { get; set; }
	public AuthStage AuthStage { get; set; }
	public CookieJar CookieJar { get; set; }
	public string UserAgent { get; set; } = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";
	public string MFACsrfToken { get; set; }
}