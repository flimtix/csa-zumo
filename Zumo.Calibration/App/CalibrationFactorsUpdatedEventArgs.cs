namespace Zumo.Calibration.App;

public sealed class CalibrationFactorsUpdatedEventArgs : EventArgs
{
    public CalibrationFactorsUpdatedEventArgs(ushort? distanceFactor, ushort? rotationFactor)
    {
        DistanceFactor = distanceFactor;
        RotationFactor = rotationFactor;
    }

    public ushort? DistanceFactor { get; }
    public ushort? RotationFactor { get; }
}