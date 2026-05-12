using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Lidar;
using Zumo.Library.Sensing;
using Zumo.RemoteControl.Configuration;

namespace Zumo.RemoteControl.Lidar;

public sealed class LidarDistanceService : ILidarDistanceService
{
    private readonly ILidar lidar;
    private readonly ILidarAnalyzer analyzer;
    private readonly ILogger<LidarDistanceService> logger;
    private readonly RemoteControlOptions options;

    public LidarDistanceService(
        ILidar lidar,
        ILidarAnalyzer analyzer,
        ILogger<LidarDistanceService> logger,
        IOptions<RemoteControlOptions> options)
    {
        this.lidar = lidar ?? throw new ArgumentNullException(nameof(lidar));
        this.analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value ?? throw new ArgumentException("Remote control options must not be null.", nameof(options));
    }

    public async Task<LidarDistanceSnapshot> CaptureAsync(CancellationToken ct = default)
    {
        var result = LidarDistanceSnapshot.Empty;
        int maxRetries = Math.Max(1, options.Lidar.SnapshotMaxRetries);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            IReadOnlyList<LidarPoint> scan = await lidar.GetFullScanAsync(ct);
            var snapshot = new LidarSnapshot(scan, DateTime.UtcNow);
            var frontSector = analyzer.AnalyzeSector(
                snapshot,
                options.Lidar.FrontSectorStartAngleDegrees,
                options.Lidar.FrontSectorEndAngleDegrees);

            bool hasAnySample = frontSector.SampleCount > 0;
            int frontDistance = hasAnySample ? frontSector.MinDistanceMm : 0;
            result = new LidarDistanceSnapshot(frontDistance, hasAnySample);

            if (hasAnySample)
            {
                break;
            }

            logger.LogDebug("Lidar snapshot invalid on attempt {Attempt}/{MaxRetries}.", attempt, maxRetries);

            if (attempt < maxRetries)
            {
                await Task.Delay(options.Lidar.SnapshotRetryDelay, ct);
            }
        }

        if (!result.HasAnySample)
        {
            logger.LogWarning("Lidar snapshot remained invalid after retries.");
        }

        return result;
    }

    public async Task<int> GetFrontDistanceMmAsync(CancellationToken ct = default)
    {
        var snapshot = await CaptureAsync(ct);
        return snapshot.FrontDistanceMm;
    }
}
