using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.RemoteControl.Execution;

public sealed class UartResponseRecorder : IDisposable
{
    private const byte DriveAddress = 0x24;
    private const byte PingAddress = 0xD1;

    private readonly IZumoConnectionTransport transport;
    private readonly IProtocolCodec codec;
    private readonly List<RecordedResponse> responses = [];
    private readonly Lock sync = new();
    private bool recording;
    private bool disposed;

    public UartResponseRecorder(IZumoConnectionTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        codec = new ZumoProtocolCodec();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!recording)
        {
            recording = true;
            transport.RawLineReceived += OnRawLineReceived;
        }
    }

    public void Stop()
    {
        if (recording)
        {
            recording = false;
            transport.RawLineReceived -= OnRawLineReceived;
        }
    }

    public IEnumerable<string> GetRecordedLines()
    {
        using (sync.EnterScope())
        {
            return responses.Select(r => r.RawLine).ToArray();
        }
    }

    private void OnRawLineReceived(object? sender, string line)
    {
        if (!codec.TryParseLine(line, out ProtocolFrame frame, out _))
        {
            return;
        }

        if (frame.Type != MessageType.SetResponse)
        {
            return;
        }

        using (sync.EnterScope())
        {
            responses.Add(new RecordedResponse(frame.Address, frame.RawLine));
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Stop();
        }
    }
}