using Zumo.Library.Lidar;

namespace Zumo.Library.Sensing;

/// <summary>
/// Immutable, thread-safe snapshot of a complete LiDAR 360° point cloud at a specific moment in time.
/// Used to ensure consistent decisions across multiple sensor reads in multi-threaded contexts.
/// </summary>
public sealed class LidarSnapshot
{
    private readonly IReadOnlyList<LidarPoint> points;
    private readonly DateTime capturedAtUtc;

    public LidarSnapshot(IReadOnlyList<LidarPoint> points, DateTime capturedAtUtc)
    {
        if (points is null)
        {
            throw new ArgumentNullException(nameof(points));
        }

        if (points.Count != 360)
        {
            throw new ArgumentException("LiDAR snapshot must contain exactly 360 points (one per degree).", nameof(points));
        }

        this.points = points.ToArray();
        this.capturedAtUtc = capturedAtUtc;
    }

    public DateTime CapturedAtUtc => capturedAtUtc;

    public LidarPoint GetPointAtAngle(int angle)
    {
        int normalized = NormalizeAngle(angle);
        return points[normalized];
    }

    private static int NormalizeAngle(int angle)
    {
        int normalized = angle % 360;
        if (normalized < 0)
        {
            normalized += 360;
        }

        return normalized;
    }
}
