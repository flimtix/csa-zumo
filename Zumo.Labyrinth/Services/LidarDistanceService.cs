using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Lidar;
using Zumo.Library.Sensing;

namespace Zumo.Labyrinth.Services;

public sealed class LidarDistanceService : INavigationDistanceService
{
    private readonly ILidar lidar;
    private readonly ILidarAnalyzer lidarAnalyzer;
    private readonly ILogger<LidarDistanceService> logger;
    private readonly NavigationOption config;

    public LidarDistanceService(
        ILidar lidar,
        ILidarAnalyzer lidarAnalyzer,
        ILogger<LidarDistanceService> logger,
        IOptions<NavigationOption> options)
    {
        this.lidar = lidar ?? throw new ArgumentNullException(nameof(lidar));
        this.lidarAnalyzer = lidarAnalyzer ?? throw new ArgumentNullException(nameof(lidarAnalyzer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public async Task<NavigationDistanceSnapshot> CaptureAsync()
    {
        var result = NavigationDistanceSnapshot.Empty;
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var scan = await lidar.GetFullScanAsync();
            var lidarSnapshot = new LidarSnapshot(scan, DateTime.UtcNow);
            var candidate = BuildDistanceSnapshot(lidarSnapshot);

            if (candidate.HasAnySample)
            {
                result = candidate;
                break;
            }

            logger.LogDebug("LiDAR distance snapshot invalid on attempt {Attempt}/{MaxRetries}.", attempt, maxRetries);

            if (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        if (!result.HasAnySample)
        {
            logger.LogWarning("LiDAR distance snapshot remained invalid after retries.");
        }

        return result;
    }

    public async Task<int> GetFrontDistanceMmAsync()
    {
        var snapshot = await CaptureAsync();
        var distance = snapshot.FrontDistanceMm;
        return distance;
    }

    public async Task<int> GetRightDistanceMmAsync()
    {
        var snapshot = await CaptureAsync();
        var distance = snapshot.RightDistanceMm;
        return distance;
    }

    public async Task<int> GetFrontLeftDistanceMmAsync()
    {
        var snapshot = await CaptureAsync();
        var distance = snapshot.FrontLeftDistanceMm;
        return distance;
    }

    public async Task<int> GetFrontRightDistanceMmAsync()
    {
        var snapshot = await CaptureAsync();
        var distance = snapshot.FrontRightDistanceMm;
        return distance;
    }

    private NavigationDistanceSnapshot BuildDistanceSnapshot(LidarSnapshot snapshot)
    {
        var frontSector = lidarAnalyzer.AnalyzeSector(
            snapshot,
            config.Sensor.FrontSectorStartAngleDegrees,
            config.Sensor.FrontSectorEndAngleDegrees);

        var rightSector = lidarAnalyzer.AnalyzeSector(
            snapshot,
            config.Sensor.RightSectorStartAngleDegrees,
            config.Sensor.RightSectorEndAngleDegrees);

        var frontLeftSector = lidarAnalyzer.AnalyzeSector(
            snapshot,
            config.Sensor.FrontLeftSectorStartAngleDegrees,
            config.Sensor.FrontLeftSectorEndAngleDegrees);

        var frontRightSector = lidarAnalyzer.AnalyzeSector(
            snapshot,
            config.Sensor.FrontRightSectorStartAngleDegrees,
            config.Sensor.FrontRightSectorEndAngleDegrees);

        var hasAnySample = frontSector.SampleCount > 0
            || rightSector.SampleCount > 0
            || frontLeftSector.SampleCount > 0
            || frontRightSector.SampleCount > 0;

        var result = new NavigationDistanceSnapshot(
            frontDistanceMm: ToMinDistance(frontSector),
            rightDistanceMm: ToAverageDistance(rightSector),
            frontLeftDistanceMm: ToMinDistance(frontLeftSector),
            frontRightDistanceMm: ToMinDistance(frontRightSector),
            hasAnySample: hasAnySample);

        logger.LogTrace(
            "Distance snapshot: front={FrontDistanceMm} right={RightDistanceMm} left={LeftDistanceMm} rightSide={RightSideDistanceMm} samples={HasAnySample}",
            result.FrontDistanceMm,
            result.RightDistanceMm,
            result.FrontLeftDistanceMm,
            result.FrontRightDistanceMm,
            result.HasAnySample);

        return result;
    }

    private static int ToMinDistance(LidarSector sector)
    {
        var distance = 0;
        if (sector.SampleCount > 0)
        {
            distance = sector.MinDistanceMm;
        }

        return distance;
    }

    private static int ToAverageDistance(LidarSector sector)
    {
        var distance = 0;
        if (sector.SampleCount > 0)
        {
            distance = sector.AverageDistanceMm;
        }

        return distance;
    }
}
