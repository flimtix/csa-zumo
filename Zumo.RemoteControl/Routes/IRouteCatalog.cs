namespace Zumo.RemoteControl.Routes;

public interface IRouteCatalog
{
    IEnumerable<RouteDefinition> GetAllRoutes();
    bool TryGetRoute(string code, out RouteDefinition route);
}
