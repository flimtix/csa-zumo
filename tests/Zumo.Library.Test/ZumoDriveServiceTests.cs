using Moq;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;
using Zumo.Library.Drive;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoDriveServiceTests
{
    [TestMethod]
    public void SetWheelSpeed_SendsSetRequestWithEncodedPayload()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoDriveService sut = new(broker.Object);

        sut.SetWheelSpeed(-120, 250, CancellationToken.None);

        broker.Verify(x => x.SendRequest(
            0x24,
            0x1,
            MessageType.SetRequest,
            "FF8800FA",
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public void GetTargetWheelSpeed_ParsesSignedValues()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x24,
                0x1,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x24, MessageType.GetResponse, 0x1, "FF8800FA", ""));

        ZumoDriveService sut = new(broker.Object);

        (short left, short right) = sut.GetTargetWheelSpeed(CancellationToken.None);

        Assert.AreEqual(-120, left);
        Assert.AreEqual(250, right);
    }

    [TestMethod]
    public void GetRemainingDistance_Throws_WhenResponseIsInvalid()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x24,
                0x2,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x24, MessageType.Event, 0x2, "0001", ""));

        ZumoDriveService sut = new(broker.Object);

        Assert.Throws<InvalidOperationException>(() => sut.GetRemainingDistance(CancellationToken.None));
    }

    [TestMethod]
    public void WaitForDistanceCompletion_ReturnsTrue_WhenDistanceReachesTolerance()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.SetupSequence(x => x.SendRequest(
                0x24,
                0x2,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x24, MessageType.GetResponse, 0x2, "0032", ""))
            .Returns(new ProtocolFrame(0x24, MessageType.GetResponse, 0x2, "0004", ""));

        ZumoDriveService sut = new(broker.Object);

        bool completed = sut.WaitForDistanceCompletion(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), CancellationToken.None);

        Assert.IsTrue(completed);
    }

    [TestMethod]
    public void WaitForDistanceCompletion_ReturnsFalse_WhenTimeoutExpires()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x24,
                0x2,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x24, MessageType.GetResponse, 0x2, "0032", ""));

        ZumoDriveService sut = new(broker.Object);

        bool completed = sut.WaitForDistanceCompletion(TimeSpan.Zero, TimeSpan.FromMilliseconds(1), CancellationToken.None);

        Assert.IsFalse(completed);
    }
}
