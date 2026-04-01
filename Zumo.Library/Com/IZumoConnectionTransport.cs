using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public interface IZumoConnectionTransport : IDisposable
{
    bool IsOpen { get; }
    event EventHandler<string>? RawLineReceived;

    void WriteRawLine(string rawLine);
}
