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
	Cloudflare = 1,

	FailedPriorToCredentialsUsed = 10,
	InvalidCredentials = 11,

	UnexpectedMfa = 20,
	FailedPriorToMfaUsed = 21,
	InvalidMfaCode = 22,

	AuthAppearedSuccessful = 30,
}
