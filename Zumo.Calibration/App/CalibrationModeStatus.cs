namespace Zumo.Calibration.App;

public sealed record CalibrationModeStatus(string Step, string Message)
{
    public static CalibrationModeStatus Initial(string modeName)
    {
        return new CalibrationModeStatus("idle", $"{modeName} is ready.");
    }
}