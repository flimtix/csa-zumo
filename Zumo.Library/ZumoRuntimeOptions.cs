using Zumo.Library.Color;
using Zumo.Library.Com;

namespace Zumo.Library;

public sealed class ZumoRuntimeOptions
{
    public ZumoConnectionTransportOptions Transport { get; init; } = new();
    public int Cm4ButtonPin { get; init; } = 27;
    public int Cm4LedPin { get; init; } = 18;
    public bool EnableLidar { get; init; }
    public int LidarMotorPin { get; init; } = 7;

    public TimeSpan DefaultRequestTimeout { get; init; } = TimeSpan.FromMilliseconds(500);
}
