namespace Zumo.Library.Button;

public interface IButton
{
    bool IsPressed { get; }
    event EventHandler<ButtonStateChangedEventArgs> ButtonChanged;
}
