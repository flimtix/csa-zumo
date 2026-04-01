using Moq;
using Zumo.Library.Color;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoColorSensorServiceTests
{
    [TestMethod]
    public void GetFloorColor_ReturnsRed_ForWrappedRedHue()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x31,
                0x0,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x31, MessageType.GetResponse, 0x0, "015E", ""));

        ZumoColorSensorService sut = new(broker.Object);

        FloorColorClassification result = sut.GetFloorColor(CancellationToken.None);

        Assert.AreEqual(FloorColorClassification.Red, result);
    }

    [TestMethod]
    public void GetFloorColor_ReturnsGreen_ForGreenHue()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x31,
                0x0,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x31, MessageType.GetResponse, 0x0, "0078", ""));

        ZumoColorSensorService sut = new(broker.Object);

        FloorColorClassification result = sut.GetFloorColor(CancellationToken.None);

        Assert.AreEqual(FloorColorClassification.Green, result);
    }

    [TestMethod]
    public void GetFloorColor_ReturnsUnknown_ForInvalidPayload()
    {
        Mock<IRequestResponseBroker> broker = new();
        broker.Setup(x => x.SendRequest(
                0x31,
                0x0,
                MessageType.GetRequest,
                string.Empty,
                TimeSpan.FromMilliseconds(500),
                CancellationToken.None))
            .Returns(new ProtocolFrame(0x31, MessageType.GetResponse, 0x0, "ZZZZ", ""));

        ZumoColorSensorService sut = new(broker.Object);

        FloorColorClassification result = sut.GetFloorColor(CancellationToken.None);

        Assert.AreEqual(FloorColorClassification.Unknown, result);
    }

    [TestMethod]
    public void ClassifyHue_UseOptionsForTolerance()
    {
        Mock<IRequestResponseBroker> broker = new();

        ZumoColorSensorService sut = new(broker.Object);

        FloorColorClassification result1 = sut.ClassifyHue(5);
        Assert.AreEqual(FloorColorClassification.Red, result1);

        FloorColorClassification result2 = sut.ClassifyHue(355);
        Assert.AreEqual(FloorColorClassification.Red, result2);

        FloorColorClassification result3 = sut.ClassifyHue(370);
        Assert.AreEqual(FloorColorClassification.Unknown, result3);
    }
}