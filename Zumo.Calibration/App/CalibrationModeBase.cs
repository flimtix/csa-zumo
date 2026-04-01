using System.Drawing;
using Zumo.Library;

namespace Zumo.Calibration.App;

public abstract class CalibrationModeBase : ICalibrationMode
{
    private readonly Lock syncLock = new();
    private CancellationTokenSource? backgroundCts;
    private Task? backgroundTask;
    private CalibrationModeStatus status;
    private CancellationTokenSource? inputWaitCts;
    private bool isWaitingForInput;

    protected CalibrationModeBase(string name, ZumoRuntime runtime)
    {
        Name = name;
        Runtime = runtime;
        status = CalibrationModeStatus.Initial(name);
    }

    public string Name { get; }
    protected ZumoRuntime Runtime { get; }

    public event EventHandler<CalibrationFactorsUpdatedEventArgs>? FactorsUpdated;

    public CalibrationModeStatus Status
    {
        get
        {
            using (syncLock.EnterScope())
            {
                return status;
            }
        }
    }

    public async ValueTask EnterAsync(CancellationToken cancellationToken)
    {
        await StopBackgroundTaskAsync();
        await OnEnterAsync(cancellationToken);
    }

    public async ValueTask ExitAsync(CancellationToken cancellationToken)
    {
        await CancelInputWaitAsync();
        await StopBackgroundTaskAsync();
        Runtime.Drive.SetWheelSpeed(0, 0, CancellationToken.None);
        Runtime.RgbLed.TurnOffTop(CancellationToken.None);
        Runtime.RgbLed.TurnOffBottom(CancellationToken.None);
        await OnExitAsync(cancellationToken);
    }

    public abstract ValueTask HandleSingleClickAsync(CancellationToken cancellationToken);

    protected virtual ValueTask OnEnterAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask OnExitAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    protected void UpdateStatus(string step, string message)
    {
        using (syncLock.EnterScope())
        {
            status = new CalibrationModeStatus(step, message);
            Console.WriteLine($"[{Name}] {message}");
        }
    }

    protected void PublishDistanceFactor(ushort factor)
    {
        FactorsUpdated?.Invoke(this, new CalibrationFactorsUpdatedEventArgs(factor, null));
    }

    protected void PublishRotationFactor(ushort factor)
    {
        FactorsUpdated?.Invoke(this, new CalibrationFactorsUpdatedEventArgs(null, factor));
    }

    protected void SetModeColor(Color color, CancellationToken cancellationToken)
    {
        Runtime.RgbLed.SetTopColor(0xFF, color, cancellationToken);
    }

    protected void TurnOffBottom(CancellationToken cancellationToken)
    {
        Runtime.RgbLed.TurnOffBottom(cancellationToken);
    }

    protected bool IsWaitingForInput
    {
        get
        {
            using (syncLock.EnterScope())
            {
                return isWaitingForInput;
            }
        }
    }

    protected async ValueTask<string?> RequestUserInputAsync(string promptMessage, CancellationToken cancellationToken)
    {
        CancellationTokenSource localInputCts;

        using (syncLock.EnterScope())
        {
            inputWaitCts?.Dispose();
            inputWaitCts = new CancellationTokenSource();
            localInputCts = inputWaitCts;
            isWaitingForInput = true;
        }

        Console.WriteLine(promptMessage);

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, localInputCts.Token);
            string? line = await Console.In.ReadLineAsync(linkedCts.Token);
            return line;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            using (syncLock.EnterScope())
            {
                isWaitingForInput = false;

                if (ReferenceEquals(inputWaitCts, localInputCts))
                {
                    inputWaitCts?.Dispose();
                    inputWaitCts = null;
                }
            }
        }
    }

    private async ValueTask CancelInputWaitAsync()
    {
        CancellationTokenSource? cts;

        using (syncLock.EnterScope())
        {
            cts = inputWaitCts;
        }

        if (cts is not null)
        {
            await cts.CancelAsync();
        }
    }

    protected void StartBackgroundTask(Func<CancellationToken, Task> taskFactory)
    {
        StopBackgroundTaskAsync().GetAwaiter().GetResult();

        backgroundCts = new CancellationTokenSource();
        CancellationToken cancellationToken = backgroundCts.Token;

        backgroundTask = Task.Run(async () =>
        {
            try
            {
                await taskFactory(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected for mode transitions.
            }
        }, cancellationToken);
    }

    protected async ValueTask StopBackgroundTaskAsync()
    {
        if (backgroundCts is null)
        {
            return;
        }

        await backgroundCts.CancelAsync();

        try
        {
            if (backgroundTask is not null)
            {
                await backgroundTask;
            }
        }
        finally
        {
            backgroundCts.Dispose();
            backgroundCts = null;
            backgroundTask = null;
        }
    }
}