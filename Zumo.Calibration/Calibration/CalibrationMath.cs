namespace Zumo.Calibration.Calibration;

public static class CalibrationMath
{
    public static ushort UpdateFactor(ushort currentFactor, double targetValue, double measuredValue)
    {
        if (targetValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetValue), "Target value must be greater than zero.");
        }

        if (measuredValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(measuredValue), "Measured value must be greater than zero.");
        }

        double raw = (targetValue / measuredValue) * currentFactor;
        double clamped = Math.Clamp(raw, 1, ushort.MaxValue);
        ushort updated = (ushort)Math.Round(clamped, MidpointRounding.AwayFromZero);
        return updated;
    }
}