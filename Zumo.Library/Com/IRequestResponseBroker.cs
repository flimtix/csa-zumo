using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public interface IRequestResponseBroker
{
    void AttachTransport(
        IZumoConnectionTransport transport,
        IProtocolCodec codec);

    void DetachTransport();

    ProtocolFrame SendRequest(
        byte address,
        byte sdf,
        MessageType requestType,
        string payload,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    void SendOneWay(
        byte address,
        byte sdf,
        string payload,
        CancellationToken cancellationToken);
}
