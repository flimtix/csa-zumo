using Zumo.RemoteControl.Routes;

namespace Zumo.RemoteControl.Test;

[TestClass]
public sealed class RouteCatalogTests
{
    [TestMethod]
    public void TryGetRoute_ReturnsRouteA()
    {
        var catalog = new RouteCatalog();

        bool found = catalog.TryGetRoute("a", out RouteDefinition route);

        Assert.IsTrue(found);
        Assert.AreEqual("A", route.Code);
        Assert.HasCount(7, route.Steps);
        Assert.AreEqual(RouteStepType.Drive, route.Steps[0].Type);
        Assert.AreEqual(500, route.Steps[0].Value);
    }
}
