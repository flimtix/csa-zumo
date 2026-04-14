namespace Zumo.Library.Sound;

public interface ISoundService
{
    void Beep(ushort frequencyHz, ushort durationMilliseconds, CancellationToken cancellationToken = default);
    void Play(SoundItem soundItem, CancellationToken cancellationToken = default);
}
