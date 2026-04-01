namespace Zumo.Library.Drive;

public interface IDriveService
{
    void DriveDistance(short distanceMm, ushort speedMmPerSecond, ushort accelerationMmPerSecondSquared, CancellationToken cancellationToken = default);
    short GetRemainingDistance(CancellationToken cancellationToken = default);
    bool WaitForDistanceCompletion(TimeSpan timeout, TimeSpan pollInterval, CancellationToken cancellationToken = default);
    void RotateInPlace(short angleDegrees, ushort speedMmPerSecond, ushort accelerationMmPerSecondSquared, CancellationToken cancellationToken = default);
    void DriveCurve(short angleDegrees, short radiusMm, ushort speedMmPerSecond, ushort accelerationMmPerSecondSquared, CancellationToken cancellationToken = default);
    void SetWheelSpeed(short leftMmPerSecond, short rightMmPerSecond, CancellationToken cancellationToken = default);
    (short LeftMmPerSecond, short RightMmPerSecond) GetTargetWheelSpeed(CancellationToken cancellationToken = default);
    void SetRotationCalibrationFactor(ushort factor, CancellationToken cancellationToken = default);
    ushort GetRotationCalibrationFactor(CancellationToken cancellationToken = default);
}