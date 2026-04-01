namespace Zumo.Library.Lidar;

using Zumo.Library.Sensing;

/// <summary>
/// Extension methods for ILidar to provide common snapshot and sector analysis operations.
/// These helpers are generic (non-domain-specific) and aid analysis, but do not implement
/// gap detection or algorithmic decision-making.
/// </summary>
public static class ILidarExtensions
{
    /// <summary>
    /// Capture a thread-safe, immutable snapshot of a fresh 360° scan.
    /// </summary>
    public static async Task<LidarSnapshot> GetSnapshotAsync(this ILidar lidar, CancellationToken ct = default)
    {
        if (lidar is null)
        {
            throw new ArgumentNullException(nameof(lidar));
        }

        var points = await lidar.GetFullScanAsync(ct).ConfigureAwait(false);
        return new LidarSnapshot(points, DateTime.UtcNow);
    }

    /// <summary>
    /// Capture a thread-safe, immutable snapshot of a fresh 360° point cloud.
    /// </summary>
    public static LidarSnapshot GetSnapshot(this ILidar lidar)
    {
        return lidar.GetSnapshotAsync().GetAwaiter().GetResult();
    }

}
