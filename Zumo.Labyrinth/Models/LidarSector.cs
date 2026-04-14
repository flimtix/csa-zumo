namespace Zumo.Labyrinth.Models;

/// <summary>
/// Analysis result of a LiDAR sector (wedge of angles).
/// Provides min/max distances and metadata for a contiguous angular region.
/// </summary>
public sealed class LidarSector
{
    public LidarSector(int minDistanceMm, int maxDistanceMm, int averageDistanceMm, int sampleCount)
    {
        if (sampleCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "Sample count cannot be negative.");
        }

        MinDistanceMm = minDistanceMm;
        MaxDistanceMm = maxDistanceMm;
        AverageDistanceMm = averageDistanceMm;
        SampleCount = sampleCount;
    }

    public int MinDistanceMm { get; }
    public int MaxDistanceMm { get; }
    public int AverageDistanceMm { get; }
    public int SampleCount { get; }
}
