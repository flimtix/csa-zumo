using System.Threading.Channels;
using Zumo.Library.Button;

namespace Zumo.Calibration.App;

public sealed class ButtonGestureInterpreter : IDisposable
{
    private readonly IButton button;
    private readonly Channel<ButtonGesture> channel;
    private readonly Lock syncLock = new();

    private readonly TimeSpan debounceWindow;
    private readonly TimeSpan doubleClickWindow;

    private DateTime lastEdgeUtc;
    private DateTime? firstClickReleasedAtUtc;
    private CancellationTokenSource? singleClickDelayCts;
    private bool disposed;

    public ButtonGestureInterpreter(
        IButton button,
        TimeSpan? debounceWindow = null,
        TimeSpan? doubleClickWindow = null)
    {
        this.button = button;
        this.debounceWindow = debounceWindow ?? TimeSpan.FromMilliseconds(45);
        this.doubleClickWindow = doubleClickWindow ?? TimeSpan.FromMilliseconds(320);

        channel = Channel.CreateUnbounded<ButtonGesture>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        button.ButtonChanged += OnButtonChanged;
    }

    public ChannelReader<ButtonGesture> Reader => channel.Reader;

    private void OnButtonChanged(object? sender, ButtonStateChangedEventArgs e)
    {
        DateTime nowUtc = DateTime.UtcNow;

        using (syncLock.EnterScope())
        {
            if (disposed)
            {
                return;
            }

            TimeSpan sinceLastEdge = nowUtc - lastEdgeUtc;
            if (sinceLastEdge < debounceWindow)
            {
                return;
            }

            lastEdgeUtc = nowUtc;

            // Count clicks on release only.
            if (e.IsPressed)
            {
                return;
            }

            if (firstClickReleasedAtUtc.HasValue)
            {
                TimeSpan sinceFirstRelease = nowUtc - firstClickReleasedAtUtc.Value;
                if (sinceFirstRelease <= doubleClickWindow)
                {
                    firstClickReleasedAtUtc = null;
                    CancelSingleClickDelayUnsafe();
                    channel.Writer.TryWrite(ButtonGesture.DoubleClick);
                    return;
                }
            }

            firstClickReleasedAtUtc = nowUtc;
            StartSingleClickDelayUnsafe();
        }
    }

    private void StartSingleClickDelayUnsafe()
    {
        CancelSingleClickDelayUnsafe();
        singleClickDelayCts = new CancellationTokenSource();
        CancellationToken cancellationToken = singleClickDelayCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(doubleClickWindow, cancellationToken);

                using (syncLock.EnterScope())
                {
                    if (disposed)
                    {
                        return;
                    }

                    if (!firstClickReleasedAtUtc.HasValue)
                    {
                        return;
                    }

                    firstClickReleasedAtUtc = null;
                }

                channel.Writer.TryWrite(ButtonGesture.SingleClick);
            }
            catch (OperationCanceledException)
            {
                // Expected when second click arrives or instance is disposed.
            }
        }, cancellationToken);
    }

    private void CancelSingleClickDelayUnsafe()
    {
        if (singleClickDelayCts is not null)
        {
            singleClickDelayCts.Cancel();
            singleClickDelayCts.Dispose();
            singleClickDelayCts = null;
        }
    }

    public void Dispose()
    {
        using (syncLock.EnterScope())
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            firstClickReleasedAtUtc = null;
            CancelSingleClickDelayUnsafe();
        }

        button.ButtonChanged -= OnButtonChanged;
        channel.Writer.TryComplete();
    }
}