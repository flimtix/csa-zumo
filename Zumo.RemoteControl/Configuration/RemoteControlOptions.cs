namespace Zumo.RemoteControl.Configuration;

public sealed class RemoteControlOptions
{
    public TcpServerOptions TcpServer { get; init; } = new();
    public HttpServerOptions HttpServer { get; init; } = new();
    public LogOptions Log { get; init; } = new();
    public LidarOptions Lidar { get; init; } = new();
    public SafetyOptions Safety { get; init; } = new();
    public DriveOptions Drive { get; init; } = new();
}

public sealed class TcpServerOptions
{
    public string BindAddress { get; init; } = "0.0.0.0";
    public int Port { get; init; } = 9000;
}

public sealed class HttpServerOptions
{
    public string BindAddress { get; init; } = "0.0.0.0";
    public int Port { get; init; } = 8080;
    public int MaxConcurrentDownloads { get; init; } = 5;
}

public sealed class LogOptions
{
    public string StudentName { get; init; } = "Student Name";
    public string TimestampFormat { get; init; } = "u";
    public string Directory { get; init; } = "./logs";
    public string FileNameFormat { get; init; } = "route-{route}_{timestamp}.txt";
    public string FileTimestampFormat { get; init; } = "yyyyMMdd_HHmmss";
    public string StopMarker { get; init; } = "...Stop";
}

public sealed class LidarOptions
{
    public int SmoothingPointCount { get; init; } = 5;
    public int FrontSectorStartAngleDegrees { get; init; } = -15;
    public int FrontSectorEndAngleDegrees { get; init; } = 15;
    public int SnapshotMaxRetries { get; init; } = 4;
    public TimeSpan SnapshotRetryDelay { get; init; } = TimeSpan.FromMilliseconds(200);
}

public sealed class SafetyOptions
{
    public int CollisionDangerDistanceMm { get; init; } = 150;
    public int PrecheckDistanceMarginMm { get; init; } = 0;
}

public sealed class DriveOptions
{
    public ushort DriveSpeedMmPerSecond { get; init; } = 100;
    public ushort DriveAccelerationMmPerSecondSquared { get; init; } = 100;
    public ushort TurnSpeedMmPerSecond { get; init; } = 200;
    public ushort TurnAccelerationMmPerSecondSquared { get; init; } = 150;
    public TimeSpan TurnSettleDelay { get; init; } = TimeSpan.FromMilliseconds(850);
    public TimeSpan DrivePollInterval { get; init; } = TimeSpan.FromMilliseconds(40);
    public TimeSpan DriveCompletionTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public int DriveCompletionToleranceMm { get; init; } = 5;
}
