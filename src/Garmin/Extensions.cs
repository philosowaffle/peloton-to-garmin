using System;

namespace Garmin
{
	public static class Extensions
	{
		public static void EnsureGarminCredentialsAreProvided(this Common.Garmin settings)
		{
			if (string.IsNullOrEmpty(settings.Email))
				throw new ArgumentException("Garmin Email must be set.", nameof(settings.Email));

			if (string.IsNullOrEmpty(settings.Password))
				throw new ArgumentException("Garmin Password must be set.", nameof(settings.Password));
		}
	}
}
