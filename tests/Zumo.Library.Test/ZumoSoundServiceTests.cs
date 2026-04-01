using Moq;
using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;
using Zumo.Library.Sound;

namespace Zumo.Library.Test;

[TestClass]
public sealed class ZumoSoundServiceTests
{
    [TestMethod]
    public void Beep_SendsEncodedFrequencyAndDuration()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoSoundService sut = new(broker.Object);

        sut.Beep(440, 1000, CancellationToken.None);

        broker.Verify(x => x.SendRequest(
            0x50,
            0x0,
            MessageType.SetRequest,
            "01B803E8",
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public void Play_SendsSingleNibbleSongIndex()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoSoundService sut = new(broker.Object);

        sut.Play(SoundItem.Bond007, CancellationToken.None);

        broker.Verify(x => x.SendRequest(
            0x50,
            0x1,
            MessageType.SetRequest,
            "7",
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public void Play_Throws_WhenSongIndexExceedsSingleNibble()
    {
        Mock<IRequestResponseBroker> broker = new();
        ZumoSoundService sut = new(broker.Object);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Play((SoundItem)16, CancellationToken.None));
    }
}
