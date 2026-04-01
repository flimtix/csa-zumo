namespace Zumo.Library.Calibration;

public sealed record CalibrationProfile
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;
    public ushort DistanceCalibrationFactor { get; init; } = 1000;
    public ushort RotationCalibrationFactor { get; init; } = 100;
    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;

    public CalibrationProfile WithDistanceFactor(ushort factor)
    {
        return this with
        {
            DistanceCalibrationFactor = factor,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public CalibrationProfile WithRotationFactor(ushort factor)
    {
        return this with
        {
            RotationCalibrationFactor = factor,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public static CalibrationProfile Default => new();
}