namespace Zumo.Library.Lidar;

public readonly struct LidarPoint
{
    public LidarPoint(ushort? distance, byte intensity)
    {
        Distance = distance;
        Intensity = intensity;
    }

    public ushort? Distance { get; }
    public byte Intensity { get; }
}