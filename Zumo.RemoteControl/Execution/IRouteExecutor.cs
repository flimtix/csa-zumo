using Zumo.RemoteControl.Routes;

namespace Zumo.RemoteControl.Execution;

public interface IRouteExecutor
{
    Task<RouteExecutionResult> ExecuteAsync(RouteDefinition route, CancellationToken ct = default);
}
