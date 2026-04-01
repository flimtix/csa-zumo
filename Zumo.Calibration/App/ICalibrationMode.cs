namespace Zumo.Calibration.App;

public interface ICalibrationMode
{
    string Name { get; }
    CalibrationModeStatus Status { get; }

    ValueTask EnterAsync(CancellationToken cancellationToken);
    ValueTask ExitAsync(CancellationToken cancellationToken);
    ValueTask HandleSingleClickAsync(CancellationToken cancellationToken);
}