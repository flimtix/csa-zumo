namespace Zumo.RemoteControl.Execution;

public sealed record RouteExecutionResult(
    string RouteCode,
    DateTimeOffset StartedAtUtc,
    RouteExecutionStatus Status,
    IEnumerable<string> UartResponses);
