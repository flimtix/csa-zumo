using System.Globalization;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Color;

public sealed class ZumoColorSensorService : IColorSensorService
{
    private const byte DefaultAddress = 0x31;
    private const byte HueSdf = 0x0;

    private readonly IRequestResponseBroker broker;
    private readonly TimeSpan requestTimeout;

    public ZumoColorSensorService(IRequestResponseBroker broker, TimeSpan? requestTimeout = null)
    {
        this.broker = broker;
        this.requestTimeout = requestTimeout ?? TimeSpan.FromMilliseconds(500);
    }

    public int? GetHue(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(
            DefaultAddress,
            HueSdf,
            MessageType.GetRequest,
            string.Empty,
            requestTimeout,
            cancellationToken);

        int? hue = ParseHue(response.Payload);
        return hue;
    }

    public FloorColorClassification GetFloorColor(CancellationToken cancellationToken)
    {
        int? hue = GetHue(cancellationToken);
        if (!hue.HasValue)
        {
            return FloorColorClassification.Unknown;
        }

        return ClassifyHue(hue.Value);
    }

    public FloorColorClassification ClassifyHue(int hue)
    {
        var classification = hue switch
        {
            < 30 => FloorColorClassification.Red,
            < 90 => FloorColorClassification.Yellow,
            < 150 => FloorColorClassification.Green,
            < 210 => FloorColorClassification.Cyan,
            < 270 => FloorColorClassification.Blue,
            < 330 => FloorColorClassification.Magenta,
            <= 360 => FloorColorClassification.Red,
            _ => FloorColorClassification.Unknown,
        };
        return classification;
    }

    private static int? ParseHue(string payload)
    {
        int? hue = null;
        bool validLength = !string.IsNullOrWhiteSpace(payload) && payload.Length >= 4;

        if (validLength)
        {
            bool parsed = int.TryParse(payload.Substring(0, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value);
            if (parsed && value >= 0 && value <= 360)
            {
                hue = value;
            }
        }

        return hue;
    }
}
