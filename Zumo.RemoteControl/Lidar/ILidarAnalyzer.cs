using Zumo.Library.Sensing;

namespace Zumo.RemoteControl.Lidar;

public interface ILidarAnalyzer
{
    LidarSector AnalyzeSector(LidarSnapshot snapshot, int startAngleDegrees, int endAngleDegrees);
}
