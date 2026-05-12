namespace Zumo.RemoteControl.Execution;

public enum RouteExecutionStatus
{
    Completed,
    PingFailed,
    StoppedByObstacle,
    Timeout,
    Cancelled,
    Failed,
}
