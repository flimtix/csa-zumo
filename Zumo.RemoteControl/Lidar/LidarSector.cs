namespace Zumo.RemoteControl.Lidar;

public readonly record struct LidarSector(int MinDistanceMm, int MaxDistanceMm, int AverageDistanceMm, int SampleCount);
