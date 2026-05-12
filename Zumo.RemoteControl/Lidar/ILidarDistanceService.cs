namespace Zumo.RemoteControl.Lidar;

public interface ILidarDistanceService
{
    Task<LidarDistanceSnapshot> CaptureAsync(CancellationToken ct = default);
    Task<int> GetFrontDistanceMmAsync(CancellationToken ct = default);
}
