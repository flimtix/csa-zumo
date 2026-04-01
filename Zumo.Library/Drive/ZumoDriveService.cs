using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Drive;

public sealed class ZumoDriveService : IDriveService
{
    private const byte DriveAddress = 0x24;
    private const byte ConstantSpeedSdf = 0x1;
    private const byte DistanceSdf = 0x2;
    private const byte CurveSdf = 0x9;
    private const byte RotateSdf = 0xA;
    private const byte RotationCalibrationSdf = 0xB;
    private const int CompletionToleranceMm = 5;

    private readonly IRequestResponseBroker broker;
    private readonly TimeSpan timeout;

    public ZumoDriveService(IRequestResponseBroker broker, TimeSpan? timeout = null)
    {
        this.broker = broker;
        this.timeout = timeout ?? TimeSpan.FromMilliseconds(500);
    }

    public void DriveDistance(short distanceMm, ushort speedMmPerSecond, ushort accelerationMmPerSecondSquared, CancellationToken cancellationToken)
    {
        string payload = HexEncoding.Int16(distanceMm) + HexEncoding.UInt16(speedMmPerSecond) + HexEncoding.UInt16(accelerationMmPerSecondSquared);
        broker.SendRequest(DriveAddress, DistanceSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }

    public short GetRemainingDistance(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(DriveAddress, DistanceSdf, MessageType.GetRequest, string.Empty, timeout, cancellationToken);
        ValidateResponse(response, DistanceSdf, 4);
        short remainingDistance = HexEncoding.ParseInt16(response.Payload.Substring(0, 4));
        return remainingDistance;
    }

    public bool WaitForDistanceCompletion(TimeSpan timeout, TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be zero or greater.");
        }

        if (pollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), "Poll interval must be greater than zero.");
        }

        DateTime deadlineUtc = DateTime.UtcNow + timeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            short remainingDistance = GetRemainingDistance(cancellationToken);
            if (Math.Abs(remainingDistance) <= CompletionToleranceMm)
            {
                return true;
            }

            if (DateTime.UtcNow >= deadlineUtc)
            {
                return false;
            }

            bool cancellationRequested = cancellationToken.WaitHandle.WaitOne(pollInterval);
            if (cancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    public void RotateInPlace(short angleDegrees, ushort speedMmPerSecond, ushort accelerationMmPerSecondSquared, CancellationToken cancellationToken)
    {
        string payload = HexEncoding.Int16(angleDegrees) + HexEncoding.UInt16(speedMmPerSecond) + HexEncoding.UInt16(accelerationMmPerSecondSquared);
        broker.SendRequest(DriveAddress, RotateSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }

    public void DriveCurve(short angleDegrees, short radiusMm, ushort speedMmPerSecond, ushort accelerationMmPerSecondSquared, CancellationToken cancellationToken)
    {
        string payload = HexEncoding.Int16(angleDegrees)
            + HexEncoding.Int16(radiusMm)
            + HexEncoding.UInt16(speedMmPerSecond)
            + HexEncoding.UInt16(accelerationMmPerSecondSquared);

        broker.SendRequest(DriveAddress, CurveSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }

    public void SetWheelSpeed(short leftMmPerSecond, short rightMmPerSecond, CancellationToken cancellationToken)
    {
        string payload = HexEncoding.Int16(leftMmPerSecond) + HexEncoding.Int16(rightMmPerSecond);
        broker.SendRequest(DriveAddress, ConstantSpeedSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }

    public (short LeftMmPerSecond, short RightMmPerSecond) GetTargetWheelSpeed(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(DriveAddress, ConstantSpeedSdf, MessageType.GetRequest, string.Empty, timeout, cancellationToken);
        ValidateResponse(response, ConstantSpeedSdf, 8);

        short left = HexEncoding.ParseInt16(response.Payload.Substring(0, 4));
        short right = HexEncoding.ParseInt16(response.Payload.Substring(4, 4));
        return (left, right);
    }

    public void SetRotationCalibrationFactor(ushort factor, CancellationToken cancellationToken)
    {
        string payload = HexEncoding.UInt16(factor);
        broker.SendRequest(DriveAddress, RotationCalibrationSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }

    public ushort GetRotationCalibrationFactor(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(DriveAddress, RotationCalibrationSdf, MessageType.GetRequest, string.Empty, timeout, cancellationToken);
        ValidateResponse(response, RotationCalibrationSdf, 4);
        ushort factor = HexEncoding.ParseUInt16(response.Payload.Substring(0, 4));
        return factor;
    }

    private static void ValidateResponse(ProtocolFrame response, byte expectedSdf, int minPayloadLength)
    {
        if (response.Type != MessageType.GetResponse && response.Type != MessageType.SetResponse)
        {
            throw new InvalidOperationException("Unexpected response type.");
        }

        if (response.Address != DriveAddress || response.Sdf != expectedSdf)
        {
            throw new InvalidOperationException("Response does not match expected drive command.");
        }

        if (response.Payload.Length < minPayloadLength)
        {
            throw new InvalidOperationException("Response payload is too short.");
        }
    }
}
