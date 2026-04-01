using Microsoft.Extensions.Options;
using Zumo.Library.Sensing;

namespace Zumo.Labyrinth.Navigation.Analyzers;

public sealed class BasicLidarAnalyzer : ILidarAnalyzer
{
    private readonly int smoothingCount;

    public BasicLidarAnalyzer(IOptions<NavigationOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        smoothingCount = options.Value?.Sensor.LidarSmoothingPointCount ?? 3;
        if (smoothingCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Lidar smoothing count must be positive.");
        }
    }

    public LidarSector AnalyzeSector(LidarSnapshot snapshot, int startAngleDegrees, int endAngleDegrees)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        int normalizedStart = NormalizeAngle(startAngleDegrees);
        int normalizedEnd = NormalizeAngle(endAngleDegrees);

        int min = int.MaxValue;
        int max = 0;
        int sum = 0;
        int count = 0;

        int angle = normalizedStart;
        while (true)
        {
            int distance = GetSmoothedDistanceMm(snapshot, angle);
            if (distance <= 0)
            {
                if (angle == normalizedEnd)
                {
                    break;
                }

                angle = NormalizeAngle(angle + 1);
                continue;
            }

            min = Math.Min(min, distance);
            max = Math.Max(max, distance);
            sum += distance;
            count++;

            if (angle == normalizedEnd)
            {
                break;
            }

            angle = NormalizeAngle(angle + 1);
        }

        int average = count == 0 ? 0 : sum / count;
        if (count == 0)
        {
            min = 0;
        }

        return new LidarSector(normalizedStart, normalizedEnd, min, max, average, count);
    }

    private int GetSmoothedDistanceMm(LidarSnapshot snapshot, int centerAngle)
    {
        int firstOffset = -(smoothingCount / 2);
        int lastOffset = firstOffset + smoothingCount - 1;

        int localMin = int.MaxValue;
        int localMax = 0;
        bool hasValidDistance = false;

        for (int offset = firstOffset; offset <= lastOffset; offset++)
        {
            int sample = snapshot.GetPointAtAngle(centerAngle + offset).Distance;
            if (sample <= 0)
            {
                continue;
            }

            localMin = Math.Min(localMin, sample);
            localMax = Math.Max(localMax, sample);
            hasValidDistance = true;
        }

        int smoothedDistance = 0;
        if (hasValidDistance)
        {
            smoothedDistance = localMin + ((localMax - localMin) / 2);
        }

        return smoothedDistance;
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
