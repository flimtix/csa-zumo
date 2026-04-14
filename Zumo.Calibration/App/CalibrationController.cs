using Zumo.Library.Calibration;

namespace Zumo.Calibration.App;

public sealed class CalibrationController
{
    private readonly CalibrationProfileFileStore profileStore;
    private readonly ButtonGestureInterpreter gestureInterpreter;
    private readonly IReadOnlyList<ICalibrationMode> modes;

    private CalibrationProfile profile;
    private int activeModeIndex;

    public CalibrationController(
        CalibrationProfileFileStore profileStore,
        CalibrationProfile profile,
        ButtonGestureInterpreter gestureInterpreter,
        IReadOnlyList<ICalibrationMode> modes)
    {
        this.profileStore = profileStore;
        this.profile = profile;
        this.gestureInterpreter = gestureInterpreter;
        this.modes = modes;

        if (modes.Count == 0)
        {
            throw new ArgumentException("At least one calibration mode is required.", nameof(modes));
        }

        foreach (ICalibrationMode mode in modes)
        {
            if (mode is CalibrationModeBase baseMode)
            {
                baseMode.FactorsUpdated += OnFactorsUpdated;
            }
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Calibration controls: single click = next step, double click = next mode.");
        Console.WriteLine($"Initial profile: distance={profile.DistanceCalibrationFactor}, rotation={profile.RotationCalibrationFactor}");

        await EnterCurrentModeAsync(cancellationToken);

        while (await gestureInterpreter.Reader.WaitToReadAsync(cancellationToken))
        {
            while (gestureInterpreter.Reader.TryRead(out ButtonGesture gesture))
            {
                switch (gesture)
                {
                    case ButtonGesture.SingleClick:
                        await CurrentMode.HandleSingleClickAsync(cancellationToken);
                        PrintModeStatus(CurrentMode);
                        break;

                    case ButtonGesture.DoubleClick:
                        await SwitchToNextModeAsync(cancellationToken);
                        break;
                }
            }
        }
    }

    private ICalibrationMode CurrentMode => modes[activeModeIndex];

    private async ValueTask SwitchToNextModeAsync(CancellationToken cancellationToken)
    {
        await CurrentMode.ExitAsync(cancellationToken);

        activeModeIndex = (activeModeIndex + 1) % modes.Count;
        await EnterCurrentModeAsync(cancellationToken);
    }

    private async ValueTask EnterCurrentModeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine($"=== Active mode: {CurrentMode.Name} ===");

        await CurrentMode.EnterAsync(cancellationToken);
        PrintModeStatus(CurrentMode);
    }

    private static void PrintModeStatus(ICalibrationMode mode)
    {
        CalibrationModeStatus status = mode.Status;
        Console.WriteLine($"[{mode.Name}] step={status.Step} | {status.Message}");
    }

    private void OnFactorsUpdated(object? sender, CalibrationFactorsUpdatedEventArgs e)
    {
        CalibrationProfile updatedProfile = profile;

        if (e.DistanceFactor.HasValue)
        {
            updatedProfile = updatedProfile.WithDistanceFactor(e.DistanceFactor.Value);
        }

        if (e.RotationFactor.HasValue)
        {
            updatedProfile = updatedProfile.WithRotationFactor(e.RotationFactor.Value);
        }

        profileStore.Save(updatedProfile);
        profile = updatedProfile;

        Console.WriteLine($"Persisted calibration profile: distance={profile.DistanceCalibrationFactor}, rotation={profile.RotationCalibrationFactor}, path={profileStore.FilePath}");
    }
}