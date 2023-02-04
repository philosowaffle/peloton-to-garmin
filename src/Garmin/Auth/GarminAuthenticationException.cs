using System;

namespace Garmin.Auth;

public class GarminAuthenticationError : Exception
{
	public Code Code { get; init; } = Code.None;
	public GarminAuthenticationError(string message) : base(message) { }
	public GarminAuthenticationError(string message, Exception innerException) : base(message, innerException) { }
}

public enum Code : byte
{
	None = 0,
	InvalidCredentials = 1,
	Cloudflare = 2,
	AuthAppearedSuccessful = 3,
	FailedPriorToCredentialsUsed = 4,
	FailedPriorToMfaUsed = 5,
	InvalidMfaCode = 6
}
