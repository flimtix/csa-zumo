using Zumo.Library.Com;
using Zumo.Library.Com.Protocol;

namespace Zumo.Library.Sound;

public sealed class ZumoSoundService : ISoundService
{
    private const byte SoundAddress = 0x50;
    private const byte BeepSdf = 0x0;
    private const byte SongSdf = 0x1;

    private readonly IRequestResponseBroker broker;
    private readonly TimeSpan timeout;

    public ZumoSoundService(IRequestResponseBroker broker, TimeSpan? timeout = null)
    {
        this.broker = broker;
        this.timeout = timeout ?? TimeSpan.FromMilliseconds(500);
    }

    public void Beep(ushort frequencyHz, ushort durationMilliseconds, CancellationToken cancellationToken)
    {
        string payload = HexEncoding.UInt16(frequencyHz) + HexEncoding.UInt16(durationMilliseconds);
        broker.SendRequest(SoundAddress, BeepSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }

    public void Play(SoundItem soundItem, CancellationToken cancellationToken)
    {
        int songIndex = (int)soundItem;
        if (songIndex is < 0 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(soundItem), "Sound item must fit in one hex nibble.");
        }

        string payload = songIndex.ToString("X1");
        broker.SendRequest(SoundAddress, SongSdf, MessageType.SetRequest, payload, timeout, cancellationToken);
    }
}
