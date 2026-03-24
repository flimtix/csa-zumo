namespace Zumo.Library.Lidar;

public interface ILidar
{
    int Speed { get; }
    LidarPoint this[int angle] { get; }
}
