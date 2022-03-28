using Common.Dto.Peloton;

namespace WebUI.Domain;

public class RecentWorkoutsGetResponse
{
	public ICollection<RecentWorkout> Items { get; set; }
}
