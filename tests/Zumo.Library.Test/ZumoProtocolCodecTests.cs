using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoProtocolCodecTests
{
    [TestMethod]
    public void TryParseLine_ParsesGetResponseWithAsterisk()
    {
        ZumoProtocolCodec sut = new();

        bool parsed = sut.TryParseLine("5*240048", out ProtocolFrame frame, out string? error);

        Assert.IsTrue(parsed);
        Assert.IsNull(error);
        Assert.AreEqual(MessageType.GetResponse, frame.Type);
        Assert.AreEqual((byte)0x24, frame.Address);
        Assert.AreEqual((byte)0x0, frame.Sdf);
        Assert.AreEqual("0048", frame.Payload);
    }

    [TestMethod]
    public void TryParseLine_ParsesSetResponseWithLessThan()
    {
        ZumoProtocolCodec sut = new();

        bool parsed = sut.TryParseLine("5<242006403E803E8", out ProtocolFrame frame, out string? error);

        Assert.IsTrue(parsed);
        Assert.IsNull(error);
        Assert.AreEqual(MessageType.SetResponse, frame.Type);
        Assert.AreEqual((byte)0x24, frame.Address);
        Assert.AreEqual((byte)0x2, frame.Sdf);
        Assert.AreEqual("006403E803E8", frame.Payload);
    }

    [TestMethod]
    public void TryParseLine_ParsesPingRequestWithoutOperator()
    {
        ZumoProtocolCodec sut = new();

        bool parsed = sut.TryParseLine("5D1Ping", out ProtocolFrame frame, out string? error);

        Assert.IsTrue(parsed);
        Assert.IsNull(error);
        Assert.AreEqual(MessageType.SetRequest, frame.Type);
        Assert.AreEqual((byte)0xD1, frame.Address);
        Assert.AreEqual((byte)0x0, frame.Sdf);
        Assert.AreEqual("Ping", frame.Payload);
    }

    [TestMethod]
    public void FormatLine_FormatsGetResponseWithAsterisk()
    {
        ZumoProtocolCodec sut = new();
        var frame = new ProtocolFrame(0x24, MessageType.GetResponse, 0x0, "0048", string.Empty);

        string line = sut.FormatLine(frame);

        Assert.AreEqual("5*240048", line);
    }

    [TestMethod]
    public void FormatLine_FormatsPingWithoutOperator()
    {
        ZumoProtocolCodec sut = new();
        var frame = new ProtocolFrame(0xD1, MessageType.SetRequest, 0x0, "Ping", string.Empty);

        string line = sut.FormatLine(frame);

        Assert.AreEqual("5D1Ping", line);
    }

    [TestMethod]
    public void TryParseLine_ParsesSetResponseWithSymbolPayload()
    {
        ZumoProtocolCodec sut = new();

        bool parsed = sut.TryParseLine("5<31$04$", out ProtocolFrame frame, out string? error);

        Assert.IsTrue(parsed);
        Assert.IsNull(error);
        Assert.AreEqual(MessageType.SetResponse, frame.Type);
        Assert.AreEqual((byte)0x31, frame.Address);
        Assert.AreEqual((byte)0x0, frame.Sdf);
        Assert.AreEqual("$04$", frame.Payload);
    }
}
