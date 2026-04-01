using Moq;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Test;

[TestClass]
public sealed class RequestResponseBrokerTests
{
    [TestMethod]
    public void SendRequest_MatchesGetResponseByAddressWhenSdfDiffersButUnique()
    {
        FakeTransport transport = new();
        ZumoProtocolCodec codec = new();

        Mock<IEventRouter> eventRouter = new();
        RequestResponseBroker sut = new(eventRouter.Object);
        sut.AttachTransport(transport, codec);

        Task<ProtocolFrame> sendTask = Task.Run(() =>
            sut.SendRequest(0x24, 0x2, MessageType.GetRequest, string.Empty, TimeSpan.FromSeconds(2), CancellationToken.None));

        WaitForWrite(transport, TimeSpan.FromSeconds(1));
        transport.EmitRawLine("5*240048");

        ProtocolFrame response = sendTask.GetAwaiter().GetResult();

        Assert.AreEqual(MessageType.GetResponse, response.Type);
        Assert.AreEqual((byte)0x24, response.Address);
        Assert.AreEqual((byte)0x2, response.Sdf);
        Assert.AreEqual("0048", response.Payload);
    }

    private static void WaitForWrite(FakeTransport transport, TimeSpan timeout)
    {
        DateTime until = DateTime.UtcNow + timeout;
        while (transport.LastWritten is null && DateTime.UtcNow < until)
        {
            Thread.Sleep(5);
        }

        if (transport.LastWritten is null)
        {
            throw new AssertFailedException("Transport write was not observed in time.");
        }
    }

    private sealed class FakeTransport : IZumoConnectionTransport
    {
        public bool IsOpen => true;
        public event EventHandler<string>? RawLineReceived;

        public string? LastWritten { get; private set; }

        public void WriteRawLine(string rawLine)
        {
            LastWritten = rawLine;
        }

        public void EmitRawLine(string rawLine)
        {
            RawLineReceived?.Invoke(this, rawLine);
        }

        public void Dispose()
        {
        }
    }
}
