namespace Zumo.Library.Lidar;

public interface ILidar
{
    Task<IReadOnlyList<LidarPoint>> GetFullScanAsync(CancellationToken ct = default);
}
