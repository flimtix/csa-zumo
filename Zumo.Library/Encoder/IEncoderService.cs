namespace Zumo.Library.Encoder;

public interface IEncoderService
{
    (short LeftMmPerSecond, short RightMmPerSecond) GetWheelSpeed(CancellationToken cancellationToken = default);
    (short LeftMm, short RightMm) GetDistance(CancellationToken cancellationToken = default);
    void ResetDistance(CancellationToken cancellationToken = default);
    void SetDistanceCalibrationFactor(ushort factor, CancellationToken cancellationToken = default);
    ushort GetDistanceCalibrationFactor(CancellationToken cancellationToken = default);
}
