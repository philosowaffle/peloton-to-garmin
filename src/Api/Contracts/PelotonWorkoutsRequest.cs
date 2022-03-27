using Common.Dto.Peloton;

namespace Api.Contracts
{
	public class RecentWorkoutsGetResponse
	{
		public ICollection<RecentWorkout> Items { get; set; }
	}
}
