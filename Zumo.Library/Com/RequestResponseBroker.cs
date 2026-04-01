using System.Collections.Concurrent;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Com;

public sealed class RequestResponseBroker : IRequestResponseBroker
{
    private readonly IEventRouter eventRouter;
    private readonly ConcurrentDictionary<RequestKey, PendingRequest> pendingRequests = [];
    private readonly Lock syncLock = new();

    private IZumoConnectionTransport? transport;
    private IProtocolCodec? codec;

    private static MessageType GetExpectedResponseType(MessageType requestType)
    {
        return requestType == MessageType.GetRequest ? MessageType.GetResponse : MessageType.SetResponse;
    }

    public RequestResponseBroker(IEventRouter eventRouter)
    {
        this.eventRouter = eventRouter;
    }

    public void AttachTransport(
        IZumoConnectionTransport transport,
        IProtocolCodec codec)
    {
        if (transport is null)
        {
            throw new ArgumentNullException(nameof(transport));
        }

        if (codec is null)
        {
            throw new ArgumentNullException(nameof(codec));
        }

        using (syncLock.EnterScope())
        {
            if (this.transport is not null)
            {
                this.transport.RawLineReceived -= OnRawLineReceived;
            }

            this.transport = transport;
            this.codec = codec;
            this.transport.RawLineReceived += OnRawLineReceived;
        }
    }

    public void DetachTransport()
    {
        using (syncLock.EnterScope())
        {
            if (transport is not null)
            {
                transport.RawLineReceived -= OnRawLineReceived;
            }

            transport = null;
            codec = null;
        }

        foreach (KeyValuePair<RequestKey, PendingRequest> entry in pendingRequests)
        {
            entry.Value.TrySetException(new InvalidOperationException("Broker detached."));
        }

        pendingRequests.Clear();
    }

    public ProtocolFrame SendRequest(
        byte address,
        byte sdf,
        MessageType requestType,
        string payload,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (requestType is not MessageType.GetRequest and not MessageType.SetRequest)
        {
            throw new ArgumentOutOfRangeException(nameof(requestType), "Request type must be GetRequest or SetRequest.");
        }

        IZumoConnectionTransport activeTransport;
        IProtocolCodec activeCodec;

        using (syncLock.EnterScope())
        {
            if (transport is null || codec is null)
            {
                throw new InvalidOperationException("Broker is not attached to a transport and codec.");
            }

            activeTransport = transport;
            activeCodec = codec;
        }

        var requestFrame = new ProtocolFrame(address, requestType, sdf, payload, string.Empty);
        var key = new RequestKey(address, sdf, GetExpectedResponseType(requestType));
        var pending = new PendingRequest();
        bool added = pendingRequests.TryAdd(key, pending);
        if (!added)
        {
            throw new InvalidOperationException($"A pending request for address {address:X2}, SDF {sdf:X1}, type {requestType} already exists.");
        }

        ProtocolFrame response;
        try
        {
            string line = activeCodec.FormatLine(requestFrame);

            activeTransport.WriteRawLine(line);
            response = pending.Wait(timeout, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new TimeoutException($"Timed out waiting for response: address {address:X2}, SDF {sdf:X1}.", ex);
        }
        finally
        {
            pendingRequests.TryRemove(key, out _);
            pending.Dispose();
        }

        return response;
    }

    public void SendOneWay(
        byte address,
        byte sdf,
        string payload,
        CancellationToken cancellationToken)
    {
        IZumoConnectionTransport activeTransport;
        IProtocolCodec activeCodec;

        using (syncLock.EnterScope())
        {
            if (transport is null || codec is null)
            {
                throw new InvalidOperationException("Broker is not attached to a transport and codec.");
            }

            activeTransport = transport;
            activeCodec = codec;
        }

        var frame = new ProtocolFrame(address, MessageType.OneWay, sdf, payload, string.Empty);
        string line = activeCodec.FormatLine(frame);
        cancellationToken.ThrowIfCancellationRequested();
        activeTransport.WriteRawLine(line);
    }

    private void OnRawLineReceived(object? sender, string rawLine)
    {
        try
        {
            HandleRawLine(rawLine);
        }
        catch
        {
            // Keep transport reader thread alive even if broker consumers fail.
        }
    }

    private void HandleRawLine(string rawLine)
    {
        IProtocolCodec? activeCodec;

        using (syncLock.EnterScope())
        {
            activeCodec = codec;
        }

        if (activeCodec is null)
        {
            return;
        }

        bool parsed = activeCodec.TryParseLine(rawLine, out ProtocolFrame frame, out _);
        if (!parsed)
        {
            return;
        }

        bool handledByPending = TryHandlePendingResponse(frame);

        if (!handledByPending)
        {
            bool shouldRoute = frame.Type is MessageType.Event or MessageType.OneWay;
            if (shouldRoute)
            {
                eventRouter.Route(frame);
            }
        }
    }

    private bool TryHandlePendingResponse(ProtocolFrame frame)
    {
        bool isResponse;
        isResponse = frame.Type is MessageType.GetResponse or MessageType.SetResponse;

        if (!isResponse)
        {
            return false;
        }

        // 1) Fast path: exact correlation (Address + Sdf + Type)
        var exactKey = new RequestKey(frame.Address, frame.Sdf, frame.Type);
        bool foundExact = pendingRequests.TryGetValue(exactKey, out PendingRequest? exactPending);
        if (foundExact && exactPending is not null)
        {
            exactPending.TrySetResult(frame);
            return true;
        }

        // 2) Fallback: some UART responses omit/flatten SDF (e.g. "5*2400BB" => parsed SDF=0).
        //    If there is exactly one pending request for this (Address + ResponseType),
        //    map response to that request and normalize SDF to expected pending SDF.
        RequestKey? uniqueKey = null;
        PendingRequest? uniquePending = null;

        foreach (KeyValuePair<RequestKey, PendingRequest> entry in pendingRequests)
        {
            if (entry.Key.Address == frame.Address && entry.Key.ResponseType == frame.Type)
            {
                if (uniquePending is not null)
                {
                    // Not unique -> ambiguous, do not route incorrectly.
                    return false;
                }

                uniqueKey = entry.Key;
                uniquePending = entry.Value;
            }
        }

        if (uniquePending is null || uniqueKey is null)
        {
            return false;
        }

        ProtocolFrame normalizedFrame = new ProtocolFrame(
            frame.Address,
            frame.Type,
            uniqueKey.Value.Sdf,
            frame.Payload,
            frame.RawLine);

        uniquePending.TrySetResult(normalizedFrame);
        return true;
    }

    private readonly record struct RequestKey(byte Address, byte Sdf, MessageType ResponseType);

    private sealed class PendingRequest : IDisposable
    {
        private readonly ManualResetEventSlim waitHandle = new(false);
        private ProtocolFrame response;
        private Exception? exception;

        public ProtocolFrame Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            bool signaled = waitHandle.Wait(timeout, cancellationToken);
            if (!signaled)
            {
                throw new TimeoutException("Timed out waiting for response.");
            }

            if (exception is not null)
            {
                throw exception;
            }

            return response;
        }

        public void TrySetResult(ProtocolFrame frame)
        {
            response = frame;
            waitHandle.Set();
        }

        public void TrySetException(Exception exception)
        {
            this.exception = exception;
            waitHandle.Set();
        }

        public void Dispose()
        {
            waitHandle.Dispose();
        }
    }
}
