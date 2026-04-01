namespace Zumo.Library.Com;

public sealed class ZumoConnectionTransportOptions
{
    public string PortName { get; init; } = "/dev/ttyAMA3";
    public int BaudRate { get; init; } = 115200;
    public int ReadTimeoutMilliseconds { get; init; } = 250;
    public string NewLine { get; init; } = "\n";
}
