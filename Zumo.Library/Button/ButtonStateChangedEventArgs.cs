namespace Zumo.Library.Button;

public class ButtonStateChangedEventArgs
{
    public ButtonStateChangedEventArgs(bool isPressed)
    {
        IsPressed = isPressed;
    }

    public bool IsPressed { get; }
}
