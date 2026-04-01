using System.Drawing;
using Zumo.Calibration.App;
using Zumo.Library;

namespace Zumo.Calibration.Modes;

public sealed class LidarOverviewMode : CalibrationModeBase
{
    private const int GroupSize = 5;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(700);

    public LidarOverviewMode(ZumoRuntime runtime)
        : base("Lidar", runtime)
    {
    }

    protected override async ValueTask OnEnterAsync(CancellationToken cancellationToken)
    {
        SetModeColor(Color.MediumPurple, cancellationToken);
        UpdateStatus("live-overview", "LiDAR live overview active. Data refreshes continuously; double click switches mode.");
        StartBackgroundTask(RunLiveOverviewLoopAsync);
        await Task.CompletedTask;
    }

    public override ValueTask HandleSingleClickAsync(CancellationToken cancellationToken)
    {
        // Live mode refreshes automatically; single click intentionally does nothing.
        return ValueTask.CompletedTask;
    }

    private async Task RunLiveOverviewLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await RenderSnapshotAsync(cancellationToken);
            await Task.Delay(RefreshInterval, cancellationToken);
        }
    }

    private async Task RenderSnapshotAsync(CancellationToken cancellationToken)
    {
        if (Runtime.Lidar is null)
        {
            UpdateStatus("unavailable", "LiDAR is not enabled. EnableLidar must be true in runtime options.");
            return;
        }

        var points = await Runtime.Lidar.GetFullScanAsync(cancellationToken);

        Console.Clear();
        Console.WriteLine("+---------------------------------------------------------------------+");
        Console.WriteLine("|                       LiDAR Live (5er)                              |");
        Console.WriteLine("+---------------------------------------------------------------------+");
        Console.WriteLine("| Angle Range | Distance avg (mm) | Intensity avg | Visual            |");
        Console.WriteLine("+---------------------------------------------------------------------+");

        for (int start = 0; start < points.Count; start += GroupSize)
        {
            int end = Math.Min(start + GroupSize - 1, points.Count - 1);

            int distanceSum = 0;
            int intensitySum = 0;
            int count = 0;

            for (int i = start; i <= end; i++)
            {
                distanceSum += points[i].Distance;
                intensitySum += points[i].Intensity;
                count++;
            }

            int avgDistance = count > 0 ? distanceSum / count : 0;
            int avgIntensity = count > 0 ? intensitySum / count : 0;

            string angleRange = $"{start:000}-{end:000}";
            string bar = BuildBar(avgDistance, avgIntensity);
            Console.WriteLine($"| {angleRange,11} | {avgDistance,17} | {avgIntensity,13} | {bar,-17} |");
        }

        Console.WriteLine("+---------------------------------------------------------------------+");
        Console.WriteLine("Double click: next mode | Refresh: automatic");
    }

    private static string BuildBar(int distance, int intensity)
    {
        int distanceBlocks = Math.Clamp(distance / 200, 0, 12);
        int intensityBlocks = Math.Clamp(intensity / 16, 0, 4);

        var distanceBar = new string('#', distanceBlocks);
        var intensityBar = new string('+', intensityBlocks);

        return $"{distanceBar}{intensityBar}";
    }
}