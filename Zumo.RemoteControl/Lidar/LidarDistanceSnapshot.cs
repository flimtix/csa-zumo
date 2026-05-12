namespace Zumo.RemoteControl.Lidar;

public sealed class LidarDistanceSnapshot
{
    public static LidarDistanceSnapshot Empty { get; } = new(0, false);

    public LidarDistanceSnapshot(int frontDistanceMm, bool hasAnySample)
    {
        FrontDistanceMm = frontDistanceMm;
        HasAnySample = hasAnySample;
    }

    public int FrontDistanceMm { get; }
    public bool HasAnySample { get; }
}
