using Microsoft.Extensions.Options;
using Zumo.Library.Lidar;
using Zumo.Library.Sensing;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Lidar;

namespace Zumo.RemoteControl.Test;

[TestClass]
public sealed class BasicLidarAnalyzerTests
{
    [TestMethod]
    public void AnalyzeSector_ComputesMinMaxAverage()
    {
        var options = Options.Create(new RemoteControlOptions
        {
            Lidar = new LidarOptions { SmoothingPointCount = 1 }
        });

        var analyzer = new BasicLidarAnalyzer(options);
        var points = new LidarPoint[360];
        points[0] = new LidarPoint(100, 1);
        points[1] = new LidarPoint(200, 1);

        var snapshot = new LidarSnapshot(points, DateTime.UtcNow);

        LidarSector sector = analyzer.AnalyzeSector(snapshot, 0, 1);

        Assert.AreEqual(100, sector.MinDistanceMm);
        Assert.AreEqual(200, sector.MaxDistanceMm);
        Assert.AreEqual(150, sector.AverageDistanceMm);
        Assert.AreEqual(2, sector.SampleCount);
    }
}
