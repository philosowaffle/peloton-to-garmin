using Common.Dto;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Conversion
{
	public class ConverterTests
	{
		[Test]
		public void GetStartTime()
		{
			var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			//return origin.AddSeconds(timestamp);
			Assert.Pass();
		}
	}
}
