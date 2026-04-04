using System.Threading.Tasks;

namespace Garmin.Auth;

public interface IServiceTicketProvider
{
	/// <summary>
	/// Returns a service ticket string, or null if not available in this deployment mode.
	/// Phase 1: always returns null (ticket arrives via API endpoint/UI paste).
	/// Phase 3: returns a ticket obtained headlessly via Playwright.
	/// </summary>
	Task<string> GetServiceTicketAsync();
}

public class ManualServiceTicketProvider : IServiceTicketProvider
{
	public Task<string> GetServiceTicketAsync() => Task.FromResult<string>(null);
}
