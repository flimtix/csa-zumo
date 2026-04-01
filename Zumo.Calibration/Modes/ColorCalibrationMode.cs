using System.Drawing;
using Zumo.Calibration.App;
using Zumo.Library;
using Zumo.Library.Com.Protocol;

namespace Zumo.Calibration.Modes;

public sealed class ColorCalibrationMode : CalibrationModeBase
{
    private const byte ColorSensorAddress = 0x31;
    private const byte ColorCalibrationSdf = 0x6;

    private ColorModeState state;

    public ColorCalibrationMode(ZumoRuntime runtime)
        : base("Color", runtime)
    {
    }

    protected override ValueTask OnEnterAsync(CancellationToken cancellationToken)
    {
        state = ColorModeState.WaitForBlackPlacement;
        TurnOffBottom(cancellationToken);
        SetModeColor(Color.Gold, cancellationToken);
        UpdateStatus("wait-black", "Place robot on black field. Single click captures black reference.");
        return ValueTask.CompletedTask;
    }

    public override async ValueTask HandleSingleClickAsync(CancellationToken cancellationToken)
    {
        switch (state)
        {
            case ColorModeState.WaitForBlackPlacement:
                ExecuteColorCalibrationCommand("00", cancellationToken);
                state = ColorModeState.WaitForWhitePlacement;
                SetModeColor(Color.Orange, cancellationToken);
                UpdateStatus("wait-white", "Black captured. Place robot on white field and single click again.");
                break;

            case ColorModeState.WaitForWhitePlacement:
                ExecuteColorCalibrationCommand("01", cancellationToken);
                state = ColorModeState.LiveTest;
                SetModeColor(Color.DeepSkyBlue, cancellationToken);
                UpdateStatus("live-test", "White captured. Live test running (200 ms). Single click restarts color mode.");
                StartBackgroundTask(RunLiveTestLoopAsync);
                break;

            case ColorModeState.LiveTest:
                await EnterAsync(cancellationToken);
                break;
        }
    }

    private void ExecuteColorCalibrationCommand(string payload, CancellationToken cancellationToken)
    {
        Runtime.Broker.SendRequest(
            ColorSensorAddress,
            ColorCalibrationSdf,
            MessageType.SetRequest,
            payload,
            TimeSpan.FromMilliseconds(500),
            cancellationToken);
    }

    private async Task RunLiveTestLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            int? hue = Runtime.ColorSensor.GetHue(cancellationToken);

            Color ledColor = hue.HasValue
                ? ColorFromHue(hue.Value)
                : Color.DarkGray;

            Console.WriteLine($"Live test hue: {(hue.HasValue ? hue.Value.ToString() : "null"),4} -> LED color: {ledColor}");

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }
    }

    private static Color ColorFromHue(int hue)
    {
        int normalizedHue = ((hue % 360) + 360) % 360;
        return normalizedHue switch
        {
            < 30 => Color.Red,
            < 90 => Color.Yellow,
            < 150 => Color.Green,
            < 210 => Color.Cyan,
            < 270 => Color.Blue,
            < 330 => Color.Magenta,
            <= 360 => Color.Red,
            _ => Color.Empty,
        };
    }

    private enum ColorModeState
    {
        WaitForBlackPlacement = 0,
        WaitForWhitePlacement = 1,
        LiveTest = 2
    }
}