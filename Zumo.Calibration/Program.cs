using Zumo.Calibration.App;
using Zumo.Calibration.Modes;
using Zumo.Library;
using Zumo.Library.Calibration;

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
	eventArgs.Cancel = true;
	cts.Cancel();
};

Console.WriteLine("Starting Zumo calibration tool. Press Ctrl+C to stop.");

CalibrationProfileFileStore profileStore = new(CalibrationProfileFileStore.GetDefaultPath());
CalibrationProfile profile = profileStore.LoadOrDefault();

using var runtime = new ZumoRuntime();

ApplyPersistedCalibration(runtime, profile, cts.Token);

ButtonGestureInterpreter gestureInterpreter = new(runtime.Cm4Button);

ColorCalibrationMode colorMode = new(runtime);
DistanceCalibrationMode distanceMode = new(runtime);
RotationCalibrationMode rotationMode = new(runtime);
LidarOverviewMode lidarMode = new(runtime);

CalibrationController controller = new(
	profileStore,
	profile,
	gestureInterpreter,
	[colorMode, distanceMode, rotationMode, lidarMode]);

try
{
	// Wait a moment for the runtime to initialize and apply calibration before starting the controller
	await Task.Delay(2000, cts.Token);
	await controller.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
	Console.WriteLine("Calibration canceled.");
}
catch (Exception ex)
{
	Console.WriteLine($"Unhandled calibration error: {ex.Message}");
}

static void ApplyPersistedCalibration(ZumoRuntime runtime, CalibrationProfile profile, CancellationToken cancellationToken)
{
	runtime.Encoder.SetDistanceCalibrationFactor(profile.DistanceCalibrationFactor, cancellationToken);
	runtime.Drive.SetRotationCalibrationFactor(profile.RotationCalibrationFactor, cancellationToken);

	Console.WriteLine(
		$"Loaded calibration profile: distance={profile.DistanceCalibrationFactor}, rotation={profile.RotationCalibrationFactor}, updated={profile.UpdatedAtUtc:O}");
}
