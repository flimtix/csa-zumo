namespace Zumo.RemoteControl.Routes;

public sealed class RouteCatalog : IRouteCatalog
{
    private readonly Lazy<IReadOnlyDictionary<string, RouteDefinition>> routes;

    public RouteCatalog()
    {
        routes = new Lazy<IReadOnlyDictionary<string, RouteDefinition>>(() => new Dictionary<string, RouteDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new RouteDefinition("A",
            [
                new(RouteStepType.Drive, 500),
                new(RouteStepType.Turn, 135),
                new(RouteStepType.Drive, 700),
                new(RouteStepType.Turn, -135),
                new(RouteStepType.Drive, 500),
                new(RouteStepType.Turn, -135),
                new(RouteStepType.Drive, 700),
            ]),
            ["B"] = new RouteDefinition("B",
            [
                new(RouteStepType.Drive, 500),
                new(RouteStepType.Turn, 90),
                new(RouteStepType.Drive, 700),
                new(RouteStepType.Turn, 135),
                new(RouteStepType.Drive, 500),
                new(RouteStepType.Turn, -135),
                new(RouteStepType.Drive, 700),
            ]),
            ["C"] = new RouteDefinition("C",
            [
                new(RouteStepType.Drive, 500),
                new(RouteStepType.Turn, 135),
                new(RouteStepType.Drive, 700),
                new(RouteStepType.Turn, 135),
                new(RouteStepType.Drive, 500),
                new(RouteStepType.Turn, 135),
                new(RouteStepType.Drive, 700),
            ]),
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IEnumerable<RouteDefinition> GetAllRoutes() => routes.Value.Values;

    public bool TryGetRoute(string code, out RouteDefinition route)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            route = null!;
            return false;
        }

        return routes.Value.TryGetValue(code.Trim(), out route!);
    }
}
