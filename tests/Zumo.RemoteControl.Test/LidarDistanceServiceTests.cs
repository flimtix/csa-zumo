using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Lidar;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Lidar;

namespace Zumo.RemoteControl.Test;

[TestClass]
public sealed class LidarDistanceServiceTests
{
    [TestMethod]
    public async Task GetFrontDistanceMmAsync_RetriesUntilValid()
    {
        var options = Options.Create(new RemoteControlOptions
        {
            Lidar = new LidarOptions
            {
                SmoothingPointCount = 1,
                FrontSectorStartAngleDegrees = 0,
                FrontSectorEndAngleDegrees = 0,
                SnapshotMaxRetries = 2,
                SnapshotRetryDelay = TimeSpan.FromMilliseconds(1),
            }
        });

        var analyzer = new BasicLidarAnalyzer(options);
        var scans = new List<IReadOnlyList<LidarPoint>>
        {
            CreateScan(null),
            CreateScan(300),
        };

        var lidar = new FakeLidar(scans);
        var logger = LoggerFactory.Create(builder => { }).CreateLogger<LidarDistanceService>();
        var service = new LidarDistanceService(lidar, analyzer, logger, options);

        int distance = await service.GetFrontDistanceMmAsync();

        Assert.AreEqual(300, distance);
    }

    private static LidarPoint[] CreateScan(ushort? distanceAtZero)
    {
        var points = new LidarPoint[360];
        if (distanceAtZero.HasValue)
        {
            points[0] = new LidarPoint(distanceAtZero.Value, 1);
        }
        return points;
    }

    private sealed class FakeLidar : ILidar
    {
        private readonly Queue<IReadOnlyList<LidarPoint>> scans;

        public FakeLidar(IEnumerable<IReadOnlyList<LidarPoint>> scans)
        {
            this.scans = new Queue<IReadOnlyList<LidarPoint>>(scans);
        }

        public Task<IReadOnlyList<LidarPoint>> GetFullScanAsync(CancellationToken ct = default)
        {
            return Task.FromResult(scans.Dequeue());
        }
    }
}
