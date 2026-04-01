using Moq;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;
using Zumo.Library.Encoder;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoEncoderServiceTests
{
    [TestMethod]
    public void GetWheelSpeed_ParsesSignedValuesFromPayload()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x22,
                0x0,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x22, MessageType.GetResponse, 0x0, "0FF8800FA", ""));

        ZumoEncoderService sut = new(broker.Object);

        (short left, short right) = sut.GetWheelSpeed(CancellationToken.None);

        Assert.AreEqual(-120, left);
        Assert.AreEqual(250, right);
    }

    [TestMethod]
    public void GetDistanceCalibrationFactor_ReturnsParsedValue()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x22,
                0x2,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x22, MessageType.GetResponse, 0x2, "03E8", ""));

        ZumoEncoderService sut = new(broker.Object);

        ushort value = sut.GetDistanceCalibrationFactor(CancellationToken.None);

        Assert.AreEqual((ushort)1000, value);
    }

    [TestMethod]
    public void SetDistanceCalibrationFactor_SendsEncodedPayload()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoEncoderService sut = new(broker.Object);

        sut.SetDistanceCalibrationFactor(1000, CancellationToken.None);

        broker.Verify(x => x.SendRequest(
            0x22,
            0x1,
            MessageType.SetRequest,
            "03E8",
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None), Times.Once);
    }
}
