using System.Device.Gpio;

namespace Zumo.Library.Led;

internal class Cm4Led : ILed, IDisposable
{
    private readonly Lock ledLock = new();
    private readonly GpioController gpio;
    private readonly int pin;
    private bool isEnabled;

    public Cm4Led(GpioController gpio, int pin)
    {
        this.pin = pin;
        this.gpio = gpio;

        gpio.OpenPin(pin, PinMode.Output, PinValue.Low);
    }

    public bool IsEnabled { get => isEnabled; set => SetState(value); }

    private void SetState(bool enable)
    {
        if (isEnabled != enable)
        {
            using (ledLock.EnterScope())
            {
                var newValue = enable ? PinValue.High : PinValue.Low;
                gpio.Write(pin, newValue);
                isEnabled = enable;
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && gpio.IsPinOpen(pin))
        {
            try
            {
                gpio.Write(pin, PinValue.Low);
            }
            finally
            {
                gpio.ClosePin(pin);
            }
        }
    }
}