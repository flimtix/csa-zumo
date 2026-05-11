using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Ping;

public sealed class PingService : IPingService
{
    private const byte PingAddress = 0xD1;
    private const byte PingSdf = 0x0;

    private readonly IRequestResponseBroker broker;
    private readonly TimeSpan timeout;

    public PingService(IRequestResponseBroker broker, TimeSpan? timeout = null)
    {
        this.broker = broker;
        this.timeout = timeout ?? TimeSpan.FromMilliseconds(500);
    }

    public bool Ping(CancellationToken cancellationToken)
    {
        ProtocolFrame response = broker.SendRequest(
            PingAddress,
            PingSdf,
            MessageType.GetRequest,
            "Ping",
            timeout,
            cancellationToken);

        var isValidResponse = response.Type == MessageType.GetResponse
            && response.Address == PingAddress
            && string.Equals(response.Payload, "Ping", StringComparison.OrdinalIgnoreCase);
        return isValidResponse;
    }
}
