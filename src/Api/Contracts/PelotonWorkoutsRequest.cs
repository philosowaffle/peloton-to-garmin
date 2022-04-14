using Common.Dto.Peloton;

namespace Api.Contracts
{
	public class RecentWorkoutsGetResponse
	{
		public RecentWorkoutsGetResponse()
		{
			Items = new List<RecentWorkout>();
		}

		public ICollection<RecentWorkout> Items { get; set; }
	}
}
