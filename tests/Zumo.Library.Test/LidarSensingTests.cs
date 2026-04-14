using Zumo.Library.Lidar;
using Zumo.Library.Sensing;

namespace Zumo.Library.Test;

[TestClass]
public sealed class LidarSnapshotTests
{
    [TestMethod]
    public void Constructor_Throws_WhenPointsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new LidarSnapshot(null!, DateTime.UtcNow));
    }

    [TestMethod]
    public void Constructor_Throws_WhenPointCountNotExactly360()
    {
        var points = new LidarPoint[359];
        Assert.Throws<ArgumentException>(() => new LidarSnapshot(points, DateTime.UtcNow));
    }

    [TestMethod]
    public void GetPointAtAngle_ReturnsCorrectPoint()
    {
        var points = new LidarPoint[360];
        points[45] = new LidarPoint(1000, 50);
        var snapshot = new LidarSnapshot(points, DateTime.UtcNow);

        var result = snapshot.GetPointAtAngle(45);

        Assert.AreEqual((ushort)1000, result.Distance);
        Assert.AreEqual(50, result.Intensity);
    }

    [TestMethod]
    public void GetPointAtAngle_HandlesNegativeAngle()
    {
        var points = new LidarPoint[360];
        points[0] = new LidarPoint(500, 25);
        var snapshot = new LidarSnapshot(points, DateTime.UtcNow);

        var result = snapshot.GetPointAtAngle(-360);

        Assert.AreEqual((ushort)500, result.Distance);
    }

    [TestMethod]
    public void Snapshot_IsImmutable_OriginalPointsCannotAffectSnapshot()
    {
        var points = new LidarPoint[360];
        points[0] = new LidarPoint(100, 10);
        var snapshot = new LidarSnapshot(points, DateTime.UtcNow);

        points[0] = new LidarPoint(999, 99);

        var result = snapshot.GetPointAtAngle(0);
        Assert.AreEqual((ushort)100, result.Distance);
    }
}
