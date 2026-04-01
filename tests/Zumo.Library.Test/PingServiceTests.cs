using Moq;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;
using Zumo.Library.Ping;

namespace Zumo.Library.Test;

[TestClass]
public sealed class PingServiceTests
{
    [TestMethod]
    public void DoPing_ReturnsTrue_WhenSetResponseMatches()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0xD1,
                0x0,
                MessageType.SetRequest,
                "Ping",
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0xD1, MessageType.SetResponse, 0x0, "Ping", ""));

        PingService sut = new(broker.Object);

        bool result = sut.DoPing(CancellationToken.None);

        Assert.IsTrue(result);
        broker.VerifyAll();
    }

    [TestMethod]
    public void DoPing_ReturnsFalse_WhenPayloadDoesNotMatch()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0xD1,
                0x0,
                MessageType.SetRequest,
                "Ping",
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0xD1, MessageType.SetResponse, 0x0, "Pong", ""));

        PingService sut = new(broker.Object);

        bool result = sut.DoPing(CancellationToken.None);

        Assert.IsFalse(result);
        broker.VerifyAll();
    }
}
