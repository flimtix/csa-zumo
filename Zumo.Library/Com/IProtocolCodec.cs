using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public interface IProtocolCodec
{
    bool TryParseLine(string rawLine, out ProtocolFrame frame, out string? error);
    string FormatLine(ProtocolFrame frame);
}
