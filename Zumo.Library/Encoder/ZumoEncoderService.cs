using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Encoder;

public sealed class ZumoEncoderService : IEncoderService
{
    private const byte EncoderAddress = 0x22;
    private const byte SpeedSdf = 0x0;
    private const byte DistanceSdf = 0x1;
    private const byte DistanceCalibrationSdf = 0x2;

    private readonly IRequestResponseBroker broker;
    private readonly TimeSpan timeout;

    public ZumoEncoderService(IRequestResponseBroker broker, TimeSpan? timeout = null)
    {
        this.broker = broker;
        this.timeout = timeout ?? TimeSpan.FromMilliseconds(500);
    }

    public (short LeftMmPerSecond, short RightMmPerSecond) GetWheelSpeed(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(EncoderAddress, SpeedSdf, MessageType.GetRequest, string.Empty, timeout, cancellationToken);
        ValidateResponse(response, SpeedSdf, 9);

        short left = HexEncoding.ParseInt16(response.Payload.Substring(1, 4));
        short right = HexEncoding.ParseInt16(response.Payload.Substring(5, 4));
        return (left, right);
    }

    public (short LeftMm, short RightMm) GetDistance(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(EncoderAddress, DistanceSdf, MessageType.GetRequest, string.Empty, timeout, cancellationToken);
        ValidateResponse(response, DistanceSdf, 9);

        short left = HexEncoding.ParseInt16(response.Payload.Substring(1, 4));
        short right = HexEncoding.ParseInt16(response.Payload.Substring(5, 4));
        return (left, right);
    }

    public void ResetDistance(CancellationToken cancellationToken)
    {
        broker.SendRequest(EncoderAddress, SpeedSdf, MessageType.SetRequest, string.Empty, timeout, cancellationToken);
    }

    public void SetDistanceCalibrationFactor(ushort factor, CancellationToken cancellationToken)
    {
        broker.SendRequest(EncoderAddress, DistanceSdf, MessageType.SetRequest, HexEncoding.UInt16(factor), timeout, cancellationToken);
    }

    public ushort GetDistanceCalibrationFactor(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(EncoderAddress, DistanceCalibrationSdf, MessageType.GetRequest, string.Empty, timeout, cancellationToken);
        ValidateResponse(response, DistanceCalibrationSdf, 4);
        ushort factor = HexEncoding.ParseUInt16(response.Payload.Substring(0, 4));
        return factor;
    }

    private static void ValidateResponse(ProtocolFrame response, byte expectedSdf, int minPayloadLength)
    {
        if (response.Type != MessageType.GetResponse && response.Type != MessageType.SetResponse)
        {
            throw new InvalidOperationException("Unexpected response type.");
        }

        if (response.Address != EncoderAddress || response.Sdf != expectedSdf)
        {
            throw new InvalidOperationException("Response does not match expected encoder command.");
        }

        if (response.Payload.Length < minPayloadLength)
        {
            throw new InvalidOperationException("Response payload is too short.");
        }
    }
}
