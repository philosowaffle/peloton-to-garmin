using System;

namespace Peloton.Dto
{
	public class PelotonAuthenticationError : Exception
	{
		public PelotonAuthenticationError(string message) : base(message) { }
		public PelotonAuthenticationError(string message, Exception innerException) : base(message, innerException) { }
	}
}
