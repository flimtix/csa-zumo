using System.Drawing;
using System.Globalization;
using Zumo.Calibration.App;
using Zumo.Library;

namespace Zumo.Calibration.Modes;

public sealed class RotationCalibrationMode : CalibrationModeBase
{
    private const double TargetAngleDegrees = 90.0;
    private const short TestTurnAngleDegrees = 90;
    private const ushort TestTurnSpeed = 200;
    private const ushort TestTurnAcceleration = 150;
    private const ushort CalibrationBaseFactor = 100;
    private static readonly TimeSpan TestTurnSettleTime = TimeSpan.FromMilliseconds(900);

    private RotationModeState state;
    private ushort activeFactor;

    public RotationCalibrationMode(ZumoRuntime runtime)
        : base("Rotation", runtime)
    {
    }

    protected override ValueTask OnEnterAsync(CancellationToken cancellationToken)
    {
        Runtime.Drive.SetWheelSpeed(0, 0, cancellationToken);

        try
        {
            activeFactor = Runtime.Drive.GetRotationCalibrationFactor(cancellationToken);
        }
        catch
        {
            activeFactor = CalibrationBaseFactor;
        }

        state = RotationModeState.WaitForReady;

        SetModeColor(Color.Gold, cancellationToken);
        UpdateStatus("wait-ready", "Place robot on start orientation and single click to run 90 degree calibration turn.");
        Console.WriteLine("Rotation calibration (manual):");
        Console.WriteLine("- Calibration run is always executed with factor 100 (0x0064).");
        Console.WriteLine("- Enter measured angle.");
        Console.WriteLine("- New factor = (Sollwinkel / Istwinkel) * 100.");
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
            case RotationModeState.WaitForReady:
                SetModeColor(Color.DeepSkyBlue, cancellationToken);
                UpdateStatus("test-turn", "Running 90 degree calibration turn with factor 100.");
                await RunCalibrationTurnWithBaseFactorAsync(cancellationToken);
                await ProcessMeasurementAsync(cancellationToken);
                break;

            case RotationModeState.WaitForVerification:
                SetModeColor(Color.DeepSkyBlue, cancellationToken);
                UpdateStatus("verify-turn", $"Running 90 degree verification turn with factor {activeFactor}.");
                await RunVerificationTurnAsync(cancellationToken);
                SetModeColor(Color.Gold, cancellationToken);
                UpdateStatus("wait-ready", "Verification done. Single click starts next calibration run.");
                state = RotationModeState.WaitForReady;
                break;
        }
    }

    private async Task ProcessMeasurementAsync(CancellationToken cancellationToken)
    {
        SetModeColor(Color.LimeGreen, cancellationToken);
        UpdateStatus("wait-input", "Waiting for measured angle input in console.");

        string? input = await RequestUserInputAsync("Bitte gemessenen Winkel in Grad eingeben:", cancellationToken);
        if (input is null)
        {
            UpdateStatus("cancelled", "Input cancelled. Single click starts a new calibration run.");
            state = RotationModeState.WaitForReady;
            return;
        }

        if (!TryParsePositiveDouble(input, out double measuredAngleDegrees))
        {
            UpdateStatus("invalid-input", "Invalid angle input. Single click starts a new calibration run.");
            state = RotationModeState.WaitForReady;
            return;
        }

        // Official formula: Faktor = (SollWinkel / IstWinkel) * 100
        double calculated = (TargetAngleDegrees / measuredAngleDegrees) * CalibrationBaseFactor;
        ushort newFactor = ToUInt16Clamped(calculated);

        Runtime.Drive.SetRotationCalibrationFactor(newFactor, cancellationToken);
        activeFactor = newFactor;
        PublishRotationFactor(newFactor);

        string factorHex = newFactor.ToString("X4");
        UpdateStatus("updated", $"Rotation factor set to {newFactor} (0x{factorHex}). Single click runs verification turn.");
        SetModeColor(Color.Gold, cancellationToken);
        state = RotationModeState.WaitForVerification;
    }

    private async Task RunCalibrationTurnWithBaseFactorAsync(CancellationToken cancellationToken)
    {
        Runtime.Drive.SetRotationCalibrationFactor(CalibrationBaseFactor, cancellationToken);
        await RunTurnAsync(cancellationToken);
    }

    private async Task RunVerificationTurnAsync(CancellationToken cancellationToken)
    {
        Runtime.Drive.SetRotationCalibrationFactor(activeFactor, cancellationToken);
        await RunTurnAsync(cancellationToken);
    }

    private async Task RunTurnAsync(CancellationToken cancellationToken)
    {
        Runtime.Drive.RotateInPlace(TestTurnAngleDegrees, TestTurnSpeed, TestTurnAcceleration, cancellationToken);
        await Task.Delay(TestTurnSettleTime, cancellationToken);
        Runtime.Drive.SetWheelSpeed(0, 0, cancellationToken);
    }

    private static bool TryParsePositiveDouble(string input, out double value)
    {
        input = input.Trim();

        if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && value > 0)
        {
            return true;
        }

        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0)
        {
            return true;
        }

        return false;
    }

    private static ushort ToUInt16Clamped(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return CalibrationBaseFactor;
        }

        int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded < ushort.MinValue)
        {
            return ushort.MinValue;
        }

        if (rounded > ushort.MaxValue)
        {
            return ushort.MaxValue;
        }

        return (ushort)rounded;
    }

    private enum RotationModeState
    {
        WaitForReady = 0,
        WaitForVerification = 1
    }
}

