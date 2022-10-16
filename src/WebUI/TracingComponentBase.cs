using Common.Observe;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace WebUI;

public class TracingComponentBase : ComponentBase, IDisposable
{
	public TracingComponentBase(string className) : base()
	{
		Activity.Current?.Dispose();
		Activity.Current = null;

		Activity.Current = Tracing.Source.CreateActivity(className, ActivityKind.Client);
	}

	public virtual void Dispose()
	{
		Activity.Current?.Dispose();
		Activity.Current = null;
	}
}
