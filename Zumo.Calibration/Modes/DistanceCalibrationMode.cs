using System.Drawing;
using Zumo.Calibration.App;
using Zumo.Calibration.Calibration;
using Zumo.Library;

namespace Zumo.Calibration.Modes;

public sealed class DistanceCalibrationMode : CalibrationModeBase
{
    private const double TargetDistanceMm = 100.0;
    private const short TestDriveDistanceMm = 100;
    private const ushort TestDriveSpeed = 90;
    private const ushort TestDriveAcceleration = 120;

    private DistanceModeState state;
    private ushort activeFactor;

    public DistanceCalibrationMode(ZumoRuntime runtime)
        : base("Distance", runtime)
    {
    }

    protected override ValueTask OnEnterAsync(CancellationToken cancellationToken)
    {
        Runtime.Drive.SetWheelSpeed(0, 0, cancellationToken);
        state = DistanceModeState.WaitForReady;
        activeFactor = Runtime.Encoder.GetDistanceCalibrationFactor(cancellationToken);

        SetModeColor(Color.Gold, cancellationToken);
        UpdateStatus("wait-ready", "Place robot on start point and single click to run 10 cm test drive.");
        Console.WriteLine("Distance calibration (manual): robot drives 100 mm, then enter the measured distance in mm.");
        return ValueTask.CompletedTask;
    }

    public override async ValueTask HandleSingleClickAsync(CancellationToken cancellationToken)
    {
        if (IsWaitingForInput)
        {
            return;
        }

        switch (state)
        {
            case DistanceModeState.WaitForReady:
                SetModeColor(Color.DeepSkyBlue, cancellationToken);
                UpdateStatus("test-drive", "Running 10 cm test drive.");
                await RunTestDriveAsync(cancellationToken);
                state = DistanceModeState.WaitForReady;
                await ProcessMeasurementAsync(cancellationToken);
                break;
        }
    }

    private async Task ProcessMeasurementAsync(CancellationToken cancellationToken)
    {
        SetModeColor(Color.LimeGreen, cancellationToken);
        UpdateStatus("wait-input", "Waiting for measured distance input in console.");

        string? input = await RequestUserInputAsync("Please enter measured distance in millimeters:", cancellationToken);
        if (input is null)
        {
            UpdateStatus("cancelled", "Input cancelled. Single click starts a new 10 cm test drive.");
            return;
        }

        bool parsed = double.TryParse(input.Trim(), out double measuredDistanceMm);
        if (!parsed || measuredDistanceMm <= 0)
        {
            UpdateStatus("invalid-input", "Invalid distance input. Enter a positive number after next test drive.");
            return;
        }

        ushort oldFactor = activeFactor;
        ushort newFactor = CalibrationMath.UpdateFactor(oldFactor, targetValue: TargetDistanceMm, measuredValue: measuredDistanceMm);
        Runtime.Encoder.SetDistanceCalibrationFactor(newFactor, cancellationToken);
        activeFactor = newFactor;
        PublishDistanceFactor(newFactor);

        string factorHex = newFactor.ToString("X4");
        UpdateStatus("updated", $"Distance factor updated: {oldFactor} -> {newFactor} (hex 0x{factorHex}). Single click runs verification drive.");
    }

    private async Task RunTestDriveAsync(CancellationToken cancellationToken)
    {
        Runtime.Drive.DriveDistance(100, 90, 120, cancellationToken);

        bool completed = Runtime.Drive.WaitForDistanceCompletion(
            timeout: TimeSpan.FromSeconds(3),
            pollInterval: TimeSpan.FromMilliseconds(40),
            cancellationToken: cancellationToken);

        if (!completed)
        {
            UpdateStatus("test-timeout", "10 cm test drive timed out.");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        Runtime.Drive.SetWheelSpeed(0, 0, cancellationToken);
    }

    private enum DistanceModeState
    {
        WaitForReady = 0
    }
}