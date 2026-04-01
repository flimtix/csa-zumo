namespace Zumo.Library.Sensing;

/// <summary>
/// Analysis result of a LiDAR sector (wedge of angles).
/// Provides min/max distances and metadata for a contiguous angular region.
/// </summary>
public sealed class LidarSector
{
    public LidarSector(int startAngleDegrees, int endAngleDegrees, int minDistanceMm, int maxDistanceMm, int averageDistanceMm, int sampleCount)
    {
        if (sampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "Sample count cannot be negative.");
        }

        StartAngleDegrees = startAngleDegrees;
        EndAngleDegrees = endAngleDegrees;
        MinDistanceMm = minDistanceMm;
        MaxDistanceMm = maxDistanceMm;
        AverageDistanceMm = averageDistanceMm;
        SampleCount = sampleCount;
    }

    public int StartAngleDegrees { get; }
    public int EndAngleDegrees { get; }
    public int MinDistanceMm { get; }
    public int MaxDistanceMm { get; }
    public int AverageDistanceMm { get; }
    public int SampleCount { get; }
}
