namespace Zumo.Library.Com.Protocol;

public readonly record struct EventSubscription(
    byte? Address,
    MessageType Type,
    byte Sdf
);
