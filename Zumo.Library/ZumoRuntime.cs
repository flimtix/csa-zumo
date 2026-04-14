using System.Device.Gpio;
using Zumo.Library.Button;
using Zumo.Library.Color;
using Zumo.Library.Com;
using Zumo.Library.Drive;
using Zumo.Library.Encoder;
using Zumo.Library.Led;
using Zumo.Library.Lidar;
using Zumo.Library.Ping;
using Zumo.Library.Sound;

namespace Zumo.Library;

public sealed class ZumoRuntime : IDisposable
{
    private readonly GpioController gpio;
    private readonly RequestResponseBroker broker;
    private bool disposed;

    public ZumoRuntime(ZumoRuntimeOptions? options = null)
    {
        ZumoRuntimeOptions runtimeOptions = options ?? new ZumoRuntimeOptions();

        gpio = new GpioController();

        Transport = new ZumoConnectionTransport(runtimeOptions.Transport);
        Codec = new ZumoProtocolCodec();
        EventRouter = new EventRouter();

        broker = new RequestResponseBroker(EventRouter);
        broker.AttachTransport(Transport, Codec);
        Broker = broker;

        Cm4Button = new Cm4Button(gpio, runtimeOptions.Cm4ButtonPin);
        Cm4Led = new Cm4Led(gpio, runtimeOptions.Cm4LedPin);

        var lidar = new Cm4Lidar(gpio, runtimeOptions.Lidar);
        lidar.SetPower(true);
        Lidar = lidar;

        Ping = new PingService(Broker, runtimeOptions.DefaultRequestTimeout);
        Drive = new ZumoDriveService(Broker, runtimeOptions.DefaultRequestTimeout);
        Encoder = new ZumoEncoderService(Broker, runtimeOptions.DefaultRequestTimeout);
        ColorSensor = new ZumoColorSensorService(Broker, runtimeOptions.DefaultRequestTimeout);
        RgbLed = new ZumoRgbLedService(Broker, runtimeOptions.DefaultRequestTimeout);
        Sound = new ZumoSoundService(Broker, runtimeOptions.DefaultRequestTimeout);
        ZumoButton = new ZumoButtonService(EventRouter);
    }

    public IZumoConnectionTransport Transport { get; }
    public IProtocolCodec Codec { get; }
    public IEventRouter EventRouter { get; }
    public IRequestResponseBroker Broker { get; }

    public IButton Cm4Button { get; }
    public ILed Cm4Led { get; }
    public ILidar Lidar { get; }

    public IPingService Ping { get; }
    public IDriveService Drive { get; }
    public IEncoderService Encoder { get; }
    public IColorSensorService ColorSensor { get; }
    public IZumoRgbLedService RgbLed { get; }
    public ISoundService Sound { get; }
    public IZumoButtonService ZumoButton { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (disposed)
        {
            return;
        }

        if (ZumoButton is IDisposable disposableZumoButton)
        {
            disposableZumoButton.Dispose();
        }

        broker.DetachTransport();

        if (Cm4Button is IDisposable disposableCm4Button)
        {
            disposableCm4Button.Dispose();
        }

        if (Cm4Led is IDisposable disposableCm4Led)
        {
            disposableCm4Led.Dispose();
        }

        if (Lidar is IDisposable disposableLidar)
        {
            disposableLidar.Dispose();
        }

        gpio.Dispose();
        Transport.Dispose();

        disposed = true;
    }
}
