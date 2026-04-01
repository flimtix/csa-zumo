using Moq;
using Zumo.Library.Button;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoButtonServiceTests
{
    [TestMethod]
    public void Constructor_Subscribes_AndDisposeUnsubscribes()
    {
        Mock<IEventRouter> eventRouter = new();
        EventSubscriptionToken token = new(Guid.NewGuid());

        eventRouter
            .Setup(x => x.Subscribe(new EventSubscription(0x61, MessageType.Event, 0x0), It.IsAny<ProtocolEventHandler>()))
            .Returns(token);

        ZumoButtonService sut = new(eventRouter.Object);

        sut.Dispose();

        eventRouter.Verify(x => x.Unsubscribe(token), Times.Once);
    }

    [TestMethod]
    public void OnEvent_UpdatesState_AndRaisesEventOnlyOnChange()
    {
        Mock<IEventRouter> eventRouter = new();
        EventSubscriptionToken token = new(Guid.NewGuid());
        ProtocolEventHandler? handler = null;

        eventRouter
            .Setup(x => x.Subscribe(new EventSubscription(0x61, MessageType.Event, 0x0), It.IsAny<ProtocolEventHandler>()))
            .Callback<EventSubscription, ProtocolEventHandler>((_, h) => handler = h)
            .Returns(token);

        using ZumoButtonService sut = new(eventRouter.Object);

        int changeCount = 0;
        bool? lastValue = null;

        sut.ButtonChanged += (_, e) =>
        {
            changeCount++;
            lastValue = e.IsPressed;
        };

        Assert.IsNotNull(handler);
        Assert.IsFalse(sut.IsPressed);

        handler!(new ProtocolFrame(0x61, MessageType.Event, 0x0, "01", ""));
        Assert.IsTrue(sut.IsPressed);
        Assert.AreEqual(1, changeCount);
        Assert.IsTrue(lastValue.HasValue && lastValue.Value);

        handler(new ProtocolFrame(0x61, MessageType.Event, 0x0, "1", ""));
        Assert.AreEqual(1, changeCount);

        handler(new ProtocolFrame(0x61, MessageType.Event, 0x0, "00", ""));
        Assert.IsFalse(sut.IsPressed);
        Assert.AreEqual(2, changeCount);
        Assert.IsFalse(lastValue.HasValue && lastValue.Value);
    }
}
