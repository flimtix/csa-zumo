namespace Zumo.Library.Com.Protocol;

public readonly record struct ProtocolFrame(
    byte Address,
    MessageType Type,
    byte Sdf,
    string Payload,
    string RawLine
);
