using System.Device.Gpio;
using System.Device.Pwm;
using System.IO.Ports;

namespace Zumo.Library.Lidar
{
    public class Cm4Lidar : ILidar, IDisposable
    {
        private const string PortName = "/dev/ttyAMA4";
        private const int PacketSize = 47;
        private const byte Header1 = 0x54;
        private const byte Header2 = 0x2C;

        // Use a 64KB buffer to prevent the OS from dropping new packets if the thread yields
        private const int SerialReadBufferSize = 65536;

        private readonly byte[] _crc8Table = {
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
        };

        private readonly SerialPort _serialPort;
        private readonly GpioController _gpio;
        private readonly int pin;
        private readonly PwmChannel _pwm;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _readTask;

        private LidarPoint[] _currentScan = new LidarPoint[360];
        private volatile TaskCompletionSource<IReadOnlyList<LidarPoint>> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _lastAngle = 0;
        private bool _disposed;

        public Cm4Lidar(GpioController gpio, int pin)
        {
            _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
            this.pin = pin;

            _gpio.OpenPin(pin, PinMode.Output);

            _pwm = PwmChannel.Create(0, 0);

            InitializeCurrentScan();

            _serialPort = new SerialPort(PortName, 230400)
            {
                ReadBufferSize = SerialReadBufferSize
            };

            _serialPort.Open();

            SetPower(true);

            // Clear any old garbage data from the OS buffer before we start parsing
            _serialPort.DiscardInBuffer();

            _readTask = Task.Run(() => ReadLoopAsync(_cts.Token));
        }

        public async Task<IReadOnlyList<LidarPoint>> GetFullScanAsync(CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            // WaitAsync allows the caller to cancel their await without destroying the shared TCS
            return await _tcs.Task.WaitAsync(ct);
        }

        public void SetPower(bool enable)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (enable)
            {
                _gpio.Write(pin, PinValue.High);
                Thread.Sleep(100);

                // 4kHz, 50% DC sets the LiDAR to Internal Speed Control Mode (Target: 10Hz)
                _pwm.Frequency = 4000;
                _pwm.DutyCycle = 0.5;
                _pwm.Start();
            }
            else
            {
                // 7kHz switches the LiDAR safely to Standby Mode
                _pwm.Frequency = 7000;
                _pwm.DutyCycle = 0.5;
                _pwm.Start();
                Thread.Sleep(100);
                _pwm.Stop();

                _gpio.Write(pin, PinValue.Low);
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            byte[] buffer = new byte[8192];
            int bufferLength = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await _serialPort.BaseStream.ReadAsync(buffer.AsMemory(bufferLength), ct);
                    if (bytesRead == 0) continue;

                    bufferLength += bytesRead;
                    bufferLength = ProcessBuffer(buffer, bufferLength);
                }
                catch (OperationCanceledException)
                {
                    break; // Normal shutdown
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LiDAR Read Error: {ex.Message}");
                    bufferLength = 0; // Clear buffer on severe error and resume
                    await Task.Delay(100, ct);
                }
            }
        }

        private int ProcessBuffer(byte[] buffer, int length)
        {
            int index = 0;

            while (index <= length - PacketSize)
            {
                if (buffer[index] == Header1 && buffer[index + 1] == Header2)
                {
                    if (VerifyCrc(buffer, index))
                    {
                        ParsePacket(buffer, index);
                        index += PacketSize;
                    }
                    else
                    {
                        index++; // CRC failed, shift by 1 and try again
                    }
                }
                else
                {
                    index++; // Not a header, shift by 1
                }
            }

            int remainingBytes = length - index;

            if (remainingBytes > 0 && index > 0)
            {
                Array.Copy(buffer, index, buffer, 0, remainingBytes);
            }
            else if (remainingBytes == buffer.Length)
            {
                // Buffer is entirely full of unparsable garbage. 
                // Return 0 so we reset the bufferLength instead of freezing in an infinite zero-read loop.
                return 0;
            }

            return remainingBytes;
        }

        private bool VerifyCrc(byte[] buffer, int startIndex)
        {
            byte crc = 0;
            for (int i = 0; i < PacketSize - 1; i++)
            {
                crc = _crc8Table[(crc ^ buffer[startIndex + i]) & 0xFF];
            }
            return crc == buffer[startIndex + PacketSize - 1];
        }

        private void ParsePacket(byte[] buffer, int startIndex)
        {
            int angleStart = buffer[startIndex + 4] | (buffer[startIndex + 5] << 8);
            int angleEnd = buffer[startIndex + 42] | (buffer[startIndex + 43] << 8);

            int step = (angleStart > angleEnd)
                ? ((36000 - angleStart) + angleEnd) / 11
                : (angleEnd - angleStart) / 11;

            for (int i = 0; i < 12; i++)
            {
                int currentAngle = ((angleStart + (i * step)) % 36000) / 100;

                // IMPORTANT: Check for rotation BEFORE assigning the point. 
                // Otherwise, the 0-degree point of the new scan is wiped out immediately.
                CheckForFullRotation(currentAngle);

                int pointOffset = startIndex + 6 + (i * 3);
                ushort distance = (ushort)(buffer[pointOffset] | (buffer[pointOffset + 1] << 8));
                byte intensity = buffer[pointOffset + 2];

                _currentScan[currentAngle] = new LidarPoint(distance, intensity);
            }
        }

        private void CheckForFullRotation(int currentAngle)
        {
            // Detect angle wrap-around indicating a full sweep
            if (currentAngle < _lastAngle && _lastAngle > 300 && currentAngle < 50)
            {
                var completedScan = new LidarPoint[360];
                Array.Copy(_currentScan, completedScan, 360);

                var oldTcs = Interlocked.Exchange(ref _tcs, new TaskCompletionSource<IReadOnlyList<LidarPoint>>(TaskCreationOptions.RunContinuationsAsynchronously));
                oldTcs.TrySetResult(completedScan);

                InitializeCurrentScan();
            }

            _lastAngle = currentAngle;
        }

        private void InitializeCurrentScan()
        {
            for (int i = 0; i < _currentScan.Length; i++)
            {
                _currentScan[i] = new LidarPoint(null, 0);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 1. Safely stop hardware
                    try { SetPower(false); } catch { /* Ignore on teardown */ }

                    // 2. Stop the background loop
                    _cts.Cancel();
                    try { _readTask.Wait(500); } catch { /* Ignore task cancellations */ }

                    // 3. Complete TCS so awaiting callers don't hang indefinitely
                    _tcs.TrySetCanceled();

                    // 4. Dispose unmanaged/managed hardware resources
                    _cts.Dispose();
                    _serialPort?.Dispose();
                    _pwm?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}