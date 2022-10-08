using Common.Dto.Api;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.Common.Api
{
	public class PagingTests
	{
		[TestCase(0, 0, ExpectedResult = false)]
		[TestCase(-1, -1, ExpectedResult = false)]
		[TestCase(-1, 0, ExpectedResult = false)]
		[TestCase(5, 100, ExpectedResult = false)]
		[TestCase(1, 0, ExpectedResult = true)]
		[TestCase(100, 5, ExpectedResult = true)]		
		public bool PagingResponseBase_Calculates_HasNext_Correctly(int pageCount, int pageIndex)
		{
			var pagedResponse = new TestPagingContract()
			{
				PageIndex = pageIndex,
				PageCount = pageCount
			};

			return pagedResponse.HasNext;
		}

		[TestCase(-1, ExpectedResult = false)]
		[TestCase(0, ExpectedResult = false)]
		[TestCase(1, ExpectedResult = true)]
		[TestCase(100, ExpectedResult = true)]
		public bool PagingResponseBase_Calculates_HasPrevious_Correctly(int pageIndex)
		{
			var pagedResponse = new TestPagingContract()
			{
				PageIndex = pageIndex,
			};

			return pagedResponse.HasPrevious;
		}
	}

	internal class TestPagingContract : PagingResponseBase<string>
	{
		public override ICollection<string> Items { get; set; }
	}
}
