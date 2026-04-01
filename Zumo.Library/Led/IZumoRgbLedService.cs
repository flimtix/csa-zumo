namespace Zumo.Library.Led;

public interface IZumoRgbLedService
{
    void SetBottomColor(byte ledMask, global::System.Drawing.Color color, CancellationToken cancellationToken = default);
    void SetTopColor(byte ledMask, global::System.Drawing.Color color, CancellationToken cancellationToken = default);
    void TurnOffBottom(CancellationToken cancellationToken = default);
    void TurnOffTop(CancellationToken cancellationToken = default);
}
