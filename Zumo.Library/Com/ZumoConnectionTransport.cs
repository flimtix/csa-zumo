using System.IO.Ports;

namespace Zumo.Library.Com;

public sealed class ZumoConnectionTransport : IZumoConnectionTransport
{
    private readonly Lock syncLock = new();
    private readonly Lock writeLock = new();

    private readonly SerialPort serialPort;
    private readonly Thread readerThread;
    private bool disposeRequested;
    private bool disposed;

    public ZumoConnectionTransport(ZumoConnectionTransportOptions? options = null)
    {
        ZumoConnectionTransportOptions resolvedOptions = options ?? new ZumoConnectionTransportOptions();

        if (string.IsNullOrWhiteSpace(resolvedOptions.PortName))
        {
            throw new ArgumentException("Port name must not be null or whitespace.", nameof(options));
        }

        if (resolvedOptions.BaudRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Baud rate must be greater than zero.");
        }

        if (string.IsNullOrEmpty(resolvedOptions.NewLine))
        {
            throw new ArgumentException("NewLine must not be null or empty.", nameof(options));
        }

        serialPort = new SerialPort(resolvedOptions.PortName, resolvedOptions.BaudRate)
        {
            NewLine = resolvedOptions.NewLine,
            ReadTimeout = resolvedOptions.ReadTimeoutMilliseconds
        };
        serialPort.Open();

        readerThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "ZumoUartReader"
        };
        readerThread.Start();
    }

    public bool IsOpen
    {
        get
        {
            bool result;
            using (syncLock.EnterScope())
            {
                result = !disposed && serialPort.IsOpen;
            }

            return result;
        }
    }

    public event EventHandler<string>? RawLineReceived;

    public void WriteRawLine(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            throw new ArgumentException("Raw line must not be null or whitespace.", nameof(rawLine));
        }

        using (writeLock.EnterScope())
        {
            bool canWrite;
            using (syncLock.EnterScope())
            {
                canWrite = !disposed && serialPort.IsOpen;
            }

            if (!canWrite)
            {
                throw new InvalidOperationException("Transport is not open.");
            }

            serialPort.WriteLine(rawLine);
        }
    }

    private void ReadLoop()
    {
        bool keepRunning = true;

        while (keepRunning)
        {
            string? line = null;

            try
            {
                if (!serialPort.IsOpen)
                {
                    keepRunning = false;
                }
                else
                {
                    line = serialPort.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        NotifyRawLineReceived(line);
                    }
                }
            }
            catch (TimeoutException)
            {
                // Continue reading.
            }
            catch (InvalidOperationException)
            {
                keepRunning = false;
            }
            catch (IOException)
            {
                keepRunning = false;
            }

            using (syncLock.EnterScope())
            {
                if (disposeRequested)
                {
                    keepRunning = false;
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            using (syncLock.EnterScope())
            {
                if (disposed)
                {
                    return;
                }

                disposeRequested = true;
                disposed = true;
            }

            if (serialPort.IsOpen)
            {
                try
                {
                    serialPort.Close();
                }
                catch
                {
                    // Ignore close errors during shutdown.
                }
            }

            if (readerThread.IsAlive)
            {
                try
                {
                    readerThread.Join(500);
                }
                catch
                {
                    // Ignore join errors during shutdown.
                }
            }

            serialPort.Dispose();
            RawLineReceived = null;
        }
    }

    private void NotifyRawLineReceived(string line)
    {
        EventHandler<string>? handlers = RawLineReceived;
        if (handlers is null)
        {
            return;
        }

        foreach (Delegate subscriber in handlers.GetInvocationList())
        {
            try
            {
                if (subscriber is EventHandler<string> handler)
                {
                    handler(this, line);
                }
            }
            catch
            {
                // Protect reader thread from consumer exceptions.
            }
        }
    }
}
