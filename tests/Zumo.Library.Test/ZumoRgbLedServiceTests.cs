using Moq;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;
using Zumo.Library.Led;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoRgbLedServiceTests
{
    [TestMethod]
    public void SetTopColor_SendsMaskAndRgbPayload()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoRgbLedService sut = new(broker.Object);

        sut.SetTopColor(0x03, global::System.Drawing.Color.FromArgb(0xAA, 0x11, 0x22), CancellationToken.None);

        broker.Verify(x => x.SendRequest(
            0x12,
            0x0,
            MessageType.SetRequest,
            "03AA1122",
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public void SetBottomColor_Throws_WhenMaskIsZero()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoRgbLedService sut = new(broker.Object);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.SetBottomColor(0x00, global::System.Drawing.Color.FromArgb(10, 20, 30), CancellationToken.None));
    }

    [TestMethod]
    public void TurnOffBottom_SendsZeroPayload()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoRgbLedService sut = new(broker.Object);

        sut.TurnOffBottom(CancellationToken.None);

        broker.Verify(x => x.SendRequest(
            0x11,
            0x0,
            MessageType.SetRequest,
            "0F",
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None), Times.Once);
    }
}
