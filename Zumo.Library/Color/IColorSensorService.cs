namespace Zumo.Library.Color;

public interface IColorSensorService
{
    int? GetHue(CancellationToken cancellationToken);
    FloorColorClassification GetFloorColor(CancellationToken cancellationToken = default);
    FloorColorClassification ClassifyHue(int hue);
}
