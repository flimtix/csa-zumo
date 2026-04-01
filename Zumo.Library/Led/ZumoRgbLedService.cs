using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Led;

public sealed class ZumoRgbLedService : IZumoRgbLedService
{
    private const byte BottomAddress = 0x11;
    private const byte TopAddress = 0x12;
    private const byte ColorSdf = 0x0;

    private readonly IRequestResponseBroker broker;
    private readonly TimeSpan timeout;

    public ZumoRgbLedService(IRequestResponseBroker broker, TimeSpan? timeout = null)
    {
        this.broker = broker;
        this.timeout = timeout ?? TimeSpan.FromMilliseconds(500);
    }

    public void SetBottomColor(byte ledMask, global::System.Drawing.Color color, CancellationToken cancellationToken)
    {
        SetColor(BottomAddress, ledMask, color, cancellationToken);
    }

    public void SetTopColor(byte ledMask, global::System.Drawing.Color color, CancellationToken cancellationToken)
    {
        SetColor(TopAddress, ledMask, color, cancellationToken);
    }

    public void TurnOffBottom(CancellationToken cancellationToken)
    {
        broker.SendRequest(BottomAddress, ColorSdf, MessageType.SetRequest, "0F", timeout, cancellationToken);
    }

    public void TurnOffTop(CancellationToken cancellationToken)
    {
        broker.SendRequest(TopAddress, ColorSdf, MessageType.SetRequest, "0F", timeout, cancellationToken);
    }

    private void SetColor(byte address, byte ledMask, global::System.Drawing.Color color, CancellationToken cancellationToken)
    {
        if (ledMask == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ledMask), "LED mask must not be zero when setting a color.");
        }

        string payload = HexEncoding.Byte(ledMask) + HexEncoding.Byte(color.R) + HexEncoding.Byte(color.G) + HexEncoding.Byte(color.B);
        broker.SendRequest(address, ColorSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }
}
