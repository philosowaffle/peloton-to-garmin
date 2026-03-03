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
	public string CookieJarString { get; set; }
	public string UserAgent { get; set; }
	public string MFACsrfToken { get; set; }
}