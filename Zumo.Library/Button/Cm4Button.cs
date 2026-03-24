using System.Device.Gpio;

namespace Zumo.Library.Button;

internal class Cm4Button : IButton, IDisposable
{
    private readonly GpioController gpio;
    private readonly int pin;

    public Cm4Button(GpioController gpio, int pin)
    {
        this.pin = pin;
        this.gpio = gpio;

        gpio.OpenPin(pin, PinMode.InputPullUp);
        gpio.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising | PinEventTypes.Falling, OnPinValueChanged);

        IsPressed = gpio.Read(pin) == PinValue.Low;
    }

    public bool IsPressed { get; private set; }
    public event EventHandler<ButtonStateChangedEventArgs>? ButtonChanged;

    private void OnPinValueChanged(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
    {
        var isPressed = pinValueChangedEventArgs.ChangeType == PinEventTypes.Falling;
        if (IsPressed != isPressed)
        {
            IsPressed = isPressed;
            ButtonChanged?.Invoke(this, new ButtonStateChangedEventArgs(isPressed));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ButtonChanged = null;

            if (gpio.IsPinOpen(pin))
            {
                try
                {
                    gpio.UnregisterCallbackForPinValueChangedEvent(pin, OnPinValueChanged);
                }
                finally
                {
                    gpio.ClosePin(pin);
                }
            }
        }
    }
}
