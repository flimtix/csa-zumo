namespace Zumo.RemoteControl.Routes;

public sealed record RouteDefinition(string Code, IReadOnlyList<RouteStep> Steps);
