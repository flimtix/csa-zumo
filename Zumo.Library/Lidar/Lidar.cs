using System.Device.Gpio;
using System.Device.Pwm;
using System.IO.Ports;

namespace Zumo.Library.Lidar;

public class Lidar : ILidar, IDisposable
{
    private const byte FIRST_HEADER_BYTE = 0x54;
    private const byte SECOND_HEADER_BYTE = 0x2C;
    private const int PACKET_SIZE = 47;

    private readonly byte[] crc8 =
    [
        0x00, 0x4d, 0x9a, 0xd7, 0x79, 0x34, 0xe3, 0xae, 0xf2, 0xbf, 0x68, 0x25, 0x8b, 0xc6, 0x11, 0x5c,
        0xa9, 0xe4, 0x33, 0x7e, 0xd0, 0x9d, 0x4a, 0x07, 0x5b, 0x16, 0xc1, 0x8c, 0x22, 0x6f, 0xb8, 0xf5,
        0x1f, 0x52, 0x85, 0xc8, 0x66, 0x2b, 0xfc, 0xb1, 0xed, 0xa0, 0x77, 0x3a, 0x94, 0xd9, 0x0e, 0x43,
        0xb6, 0xfb, 0x2c, 0x61, 0xcf, 0x82, 0x55, 0x18, 0x44, 0x09, 0xde, 0x93, 0x3d, 0x70, 0xa7, 0xea,
        0x3e, 0x73, 0xa4, 0xe9, 0x47, 0x0a, 0xdd, 0x90, 0xcc, 0x81, 0x56, 0x1b, 0xb5, 0xf8, 0x2f, 0x62,
        0x97, 0xda, 0x0d, 0x40, 0xee, 0xa3, 0x74, 0x39, 0x65, 0x28, 0xff, 0xb2, 0x1c, 0x51, 0x86, 0xcb,
        0x21, 0x6c, 0xbb, 0xf6, 0x58, 0x15, 0xc2, 0x8f, 0xd3, 0x9e, 0x49, 0x04, 0xaa, 0xe7, 0x30, 0x7d,
        0x88, 0xc5, 0x12, 0x5f, 0xf1, 0xbc, 0x6b, 0x26, 0x7a, 0x37, 0xe0, 0xad, 0x03, 0x4e, 0x99, 0xd4,
        0x7c, 0x31, 0xe6, 0xab, 0x05, 0x48, 0x9f, 0xd2, 0x8e, 0xc3, 0x14, 0x59, 0xf7, 0xba, 0x6d, 0x20,
        0xd5, 0x98, 0x4f, 0x02, 0xac, 0xe1, 0x36, 0x7b, 0x27, 0x6a, 0xbd, 0xf0, 0x5e, 0x13, 0xc4, 0x89,
        0x63, 0x2e, 0xf9, 0xb4, 0x1a, 0x57, 0x80, 0xcd, 0x91, 0xdc, 0x0b, 0x46, 0xe8, 0xa5, 0x72, 0x3f,
        0xca, 0x87, 0x50, 0x1d, 0xb3, 0xfe, 0x29, 0x64, 0x38, 0x75, 0xa2, 0xef, 0x41, 0x0c, 0xdb, 0x96,
        0x42, 0x0f, 0xd8, 0x95, 0x3b, 0x76, 0xa1, 0xec, 0xb0, 0xfd, 0x2a, 0x67, 0xc9, 0x84, 0x53, 0x1e,
        0xeb, 0xa6, 0x71, 0x3c, 0x92, 0xdf, 0x08, 0x45, 0x19, 0x54, 0x83, 0xce, 0x60, 0x2d, 0xfa, 0xb7,
        0x5d, 0x10, 0xc7, 0x8a, 0x24, 0x69, 0xbe, 0xf3, 0xaf, 0xe2, 0x35, 0x78, 0xd6, 0x9b, 0x4c, 0x01,
        0xf4, 0xb9, 0x6e, 0x23, 0x8d, 0xc0, 0x17, 0x5a, 0x06, 0x4b, 0x9c, 0xd1, 0x7f, 0x32, 0xe5, 0xa8
    ];

    private readonly GpioController gpio;
    private readonly int pin;
    private readonly PwmChannel pwm;
    private readonly SerialPort com;

    private readonly LidarPoint[] points;

    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Thread processingThread;

    internal Lidar(GpioController gpio, int pin)
    {
        this.gpio = gpio;
        this.pin = pin;

        gpio.OpenPin(pin, PinMode.Output);

        pwm = PwmChannel.Create(0, 0);

        com = new SerialPort("/dev/ttyAMA4", 230400);
        com.Open();

        points = new LidarPoint[360];

        cancellationTokenSource = new CancellationTokenSource();

        processingThread = new Thread(() => ProcessSerialStream(cancellationTokenSource.Token))
        {
            IsBackground = true,
            Name = "LidarProcessingThread"
        };
        processingThread.Start();
    }

    public int Speed { get; private set; }

    public LidarPoint this[int angle] => points[angle % 360];

    public void SetPower(bool enable)
    {
        if (enable)
        {
            gpio.Write(pin, true);
            Thread.Sleep(100);
            pwm.Frequency = 1000;
            pwm.DutyCycle = 0.5;
            pwm.Start();
            Thread.Sleep(100);
            pwm.DutyCycle = 0.15;
            Thread.Sleep(100);
            pwm.Stop();
        }
        else
        {
            pwm.Frequency = 7000;
            pwm.DutyCycle = 0.5;
            pwm.Start();
            Thread.Sleep(100);
            pwm.Stop();
            Thread.Sleep(500);
            gpio.Write(pin, false);
        }
    }

    private void ProcessSerialStream(CancellationToken cancellationToken)
    {
        var data = new byte[PACKET_SIZE];
        while (!cancellationToken.IsCancellationRequested)
        {
            var first = com.ReadByte();
            var second = com.ReadByte();

            var isHeader = first == FIRST_HEADER_BYTE && second == SECOND_HEADER_BYTE;
            if (isHeader)
            {
                data[0] = FIRST_HEADER_BYTE;
                data[1] = SECOND_HEADER_BYTE;

                ReadExactBytes(data, 2, PACKET_SIZE - 2);

                if (CalculateCrc(data) == 0)
                {
                    ProcessPacket(data);

                    ReadSynchronized(data, cancellationToken);
                }
            }
        }
    }

    private void ReadSynchronized(byte[] data, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadExactBytes(data, 0, PACKET_SIZE);

            if (data[0] != FIRST_HEADER_BYTE || data[1] != SECOND_HEADER_BYTE || CalculateCrc(data) != 0)
            {
                break;
            }

            ProcessPacket(data);
        }
    }

    private void ProcessPacket(byte[] data)
    {
        Speed = BitConverter.ToUInt16(data, 2);

        var startAngle = BitConverter.ToUInt16(data, 4) / 100.0;
        var endAngle = BitConverter.ToUInt16(data, 42) / 100.0;

        // Distance between start and end angle, considering the wrap-around at 360 degrees
        var distanceBetweenStartEnd = Math.Abs(endAngle - startAngle) % 360;

        // The packet contains 12 points. So there are 11 intermediate steps between start and end.
        var step = distanceBetweenStartEnd / 11.0;

        // After byte 6, there are 12 points, each consisting of 3 bytes (2 for distance, 1 for intensity)
        // 6 bytes for: Header (2), Speed (2), StartAngle (2)
        // 3 bytes per point: Distance (2), Intensity (1)
        for (int i = 0; i < 12; i++)
        {
            var offset = 6 + (i * 3);

            ushort distance = BitConverter.ToUInt16(data, offset);
            byte intensity = data[offset + 2];

            var currentAngle = startAngle + (step * i);
            int angleIndex = (int)Math.Round(currentAngle) % 360;

            points[angleIndex] = new LidarPoint(distance, intensity);
        }
    }

    private void ReadExactBytes(byte[] buffer, int offset, int count)
    {
        // Read waits until at least 1 byte is available, but can return fewer than requested
        // We can read in a loop until we have read the exact number of bytes we need 
        int bytesReadTotal = 0;
        while (bytesReadTotal < count)
        {
            bytesReadTotal += com.Read(buffer, offset + bytesReadTotal, count - bytesReadTotal);
        }
    }

    private byte CalculateCrc(byte[] data)
    {
        byte crc = 0;
        for (int i = 0; i < PACKET_SIZE; i++)
        {
            crc = crc8[(crc ^ data[i]) & 0xFF];
        }
        return crc;
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
            cancellationTokenSource.Cancel();
            if (processingThread.IsAlive)
            {
                try
                {
                    processingThread.Join(500);
                }
                catch
                {
                    // Ignore any exceptions during thread join, as we are disposing anyway
                }
            }
            cancellationTokenSource.Dispose();

            try
            {
                SetPower(false);
            }
            catch
            {
                // Ignore any exceptions during power down, as we are disposing anyway
            }

            if (com.IsOpen)
            {
                com.Close();
            }
            com.Dispose();

            pwm.Dispose();

            if (gpio.IsPinOpen(pin))
            {
                gpio.ClosePin(pin);
            }
        }
    }
}
