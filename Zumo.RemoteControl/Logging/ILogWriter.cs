using Zumo.RemoteControl.Execution;

namespace Zumo.RemoteControl.Logging;

public interface ILogWriter
{
    Task<string> WriteAsync(RouteExecutionResult result, CancellationToken ct = default);
}
