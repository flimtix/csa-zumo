using Microsoft.Extensions.Logging;
using Zumo.Library.Color;

namespace Zumo.Labyrinth.Navigation.Configuration;

public sealed class NavigationOption
{
    public LogLevel LogLevel { get; init; } = LogLevel.Debug;
    public NavigationBehaviorOption Navigation { get; init; } = new();
    public NavigationSensorOption Sensor { get; init; } = new();
    public NavigationSafetyOption Safety { get; init; } = new();
    public NavigationMotionOption Motion { get; init; } = new();
    public NavigationCalibrationOption Calibration { get; init; } = new();
    public LibraryOption Library { get; init; } = new();
}

public sealed class NavigationBehaviorOption
{
    public int WallThresholdMm { get; init; } = 150;
    public int GapThresholdMm { get; init; } = 250;
    public int GapCheckDriveInMm { get; init; } = 25;
    public int MissingRightWallMultiplier { get; init; } = 3;
    public int GapDetectionMaxAttempts { get; init; } = 20;
    public int GapDetectionRetryDelayMs { get; init; } = 200;
    public int SideRiskFallbackTurnDegrees { get; init; } = 25;
}

public sealed class NavigationSensorOption
{
    public int LidarSmoothingPointCount { get; init; } = 5;
    public int FrontSectorStartAngleDegrees { get; init; } = -15;
    public int FrontSectorEndAngleDegrees { get; init; } = 15;
    public int RightSectorStartAngleDegrees { get; init; } = 70;
    public int RightSectorEndAngleDegrees { get; init; } = 110;
    public int FrontLeftSectorStartAngleDegrees { get; init; } = -35;
    public int FrontLeftSectorEndAngleDegrees { get; init; } = -10;
    public int FrontRightSectorStartAngleDegrees { get; init; } = 10;
    public int FrontRightSectorEndAngleDegrees { get; init; } = 35;
}

public sealed class NavigationSafetyOption
{
    public int CollisionDangerDistanceMm { get; init; } = 150;
    public int SideCollisionDangerDistanceMm { get; init; } = 130;
    public int GapAlignmentCorrectionDegrees { get; init; } = 5;
    public int GapAlignmentMaxCorrections { get; init; } = 7;
    public int GapAlignmentRecheckDelayMs { get; init; } = 120;
}

public sealed class NavigationMotionOption
{
    public int StepSizeMm { get; init; } = 60;
    public int SafeDriveChunkMm { get; init; } = 25;
    public ushort DriveSpeedMmPerSecond { get; init; } = 90;
    public ushort DriveAccelerationMmPerSecondSquared { get; init; } = 120;
    public int DriveRightDriftCorrectionDegrees { get; init; } = 1;
    public ushort TurnSpeedMmPerSecond { get; init; } = 200;
    public ushort TurnAccelerationMmPerSecondSquared { get; init; } = 150;
    public TimeSpan DriveCompletionTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan DrivePollInterval { get; init; } = TimeSpan.FromMilliseconds(40);
    public TimeSpan TurnSettleDelay { get; init; } = TimeSpan.FromMilliseconds(850);
}

public sealed class NavigationCalibrationOption
{
    public int WallCalibrationDistanceMm { get; init; } = 80;
    public int WallCalibrationApproachStepMm { get; init; } = 15;
    public int WallAlignmentToleranceMm { get; init; } = 15;
    public int WallAlignmentTurnStepDegrees { get; init; } = 10;
    public int WallAlignmentMaxIterations { get; init; } = 15;
}

public sealed class LibraryOption
{
    public FloorColorClassification InnerRingFloorColor { get; init; } = FloorColorClassification.Blue;
    public FloorColorClassification OuterRingFloorColor { get; init; } = FloorColorClassification.Green;
}