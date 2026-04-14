using Zumo.Library.Com;

namespace Zumo.Library;

public sealed class ZumoRuntimeOptions
{
    public int Cm4ButtonPin { get; init; } = 27;
    public int Cm4LedPin { get; init; } = 18;

    public ZumoConnectionTransportOptions Transport { get; init; } = new();
    public LidarRuntimeOptions Lidar { get; init; } = new();

    public TimeSpan DefaultRequestTimeout { get; init; } = TimeSpan.FromMilliseconds(500);
}

public sealed class ZumoConnectionTransportOptions
{
    public string PortName { get; init; } = "/dev/ttyAMA3";
    public int BaudRate { get; init; } = 115200;
    public int ReadTimeoutMilliseconds { get; init; } = 250;
    public string NewLine { get; init; } = "\n";
}

public sealed class LidarRuntimeOptions
{
    public string PortName { get; init; } = "/dev/ttyAMA4";
    public int BaudRate { get; init; } = 230400;
    public int MotorPin { get; init; } = 7;
}