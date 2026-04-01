namespace Zumo.Library.Button;

public interface IZumoButtonService
{
    bool IsPressed { get; }
    event EventHandler<ButtonStateChangedEventArgs>? ButtonChanged;
}
