namespace Zumo.Labyrinth.Configuration;

public interface INavigationOptionValidator
{
    void Validate(NavigationOption option);
}

public sealed class NavigationOptionValidator : INavigationOptionValidator
{
    public void Validate(NavigationOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        ValidateRequiredSections(option);
        ValidateBehavior(option.Navigation);
        ValidateSensor(option.Sensor);
        ValidateSafety(option.Safety);
        ValidateMotion(option.Motion);
        ValidateCalibration(option.Calibration);
    }

    private static void ValidateRequiredSections(NavigationOption option)
    {
        if (option.Navigation is null)
        {
            throw new ArgumentException("Navigation section must not be null.", nameof(option));
        }

        if (option.Sensor is null)
        {
            throw new ArgumentException("Sensor section must not be null.", nameof(option));
        }

        if (option.Safety is null)
        {
            throw new ArgumentException("Safety section must not be null.", nameof(option));
        }

        if (option.Motion is null)
        {
            throw new ArgumentException("Motion section must not be null.", nameof(option));
        }

        if (option.Calibration is null)
        {
            throw new ArgumentException("Calibration section must not be null.", nameof(option));
        }
    }

    private static void ValidateBehavior(NavigationBehaviorOption behavior)
    {
        if (behavior.WallThresholdMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "WallThresholdMm must be positive.");
        }

        if (behavior.GapThresholdMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "GapThresholdMm must be positive.");
        }

        if (behavior.GapCheckDriveInMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "GapCheckDriveInMm must be positive.");
        }

        if (behavior.MissingRightWallMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "MissingRightWallMultiplier must be positive.");
        }

        if (behavior.GapDetectionMaxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "GapDetectionMaxAttempts must be positive.");
        }

        if (behavior.GapDetectionRetryDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "GapDetectionRetryDelayMs must be non-negative.");
        }

        if (behavior.SideRiskFallbackTurnDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(behavior), "SideRiskFallbackTurnDegrees must be non-negative.");
        }
    }

    private static void ValidateSensor(NavigationSensorOption sensor)
    {
        if (sensor.LidarSmoothingPointCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sensor), "LidarSmoothingPointCount must be positive.");
        }
    }

    private static void ValidateSafety(NavigationSafetyOption safety)
    {
        if (safety.CollisionDangerDistanceMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(safety), "CollisionDangerDistanceMm must be positive.");
        }

        if (safety.SideCollisionDangerDistanceMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(safety), "SideCollisionDangerDistanceMm must be positive.");
        }

        if (safety.GapAlignmentCorrectionDegrees <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(safety), "GapAlignmentCorrectionDegrees must be positive.");
        }

        if (safety.GapAlignmentMaxCorrections <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(safety), "GapAlignmentMaxCorrections must be positive.");
        }

        if (safety.GapAlignmentRecheckDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(safety), "GapAlignmentRecheckDelayMs must be non-negative.");
        }
    }

    private static void ValidateMotion(NavigationMotionOption motion)
    {
        if (motion.StepSizeMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(motion), "StepSizeMm must be positive.");
        }

        if (motion.SafeDriveChunkMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(motion), "SafeDriveChunkMm must be positive.");
        }

        if (motion.DriveRightDriftCorrectionDegrees < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(motion), "DriveRightDriftCorrectionDegrees must be non-negative.");
        }
    }

    private static void ValidateCalibration(NavigationCalibrationOption calibration)
    {
        if (calibration.WallCalibrationDistanceMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calibration), "WallCalibrationDistanceMm must be positive.");
        }

        if (calibration.WallCalibrationApproachStepMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calibration), "WallCalibrationApproachStepMm must be positive.");
        }

        if (calibration.WallAlignmentToleranceMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calibration), "WallAlignmentToleranceMm must be positive.");
        }

        if (calibration.WallAlignmentTurnStepDegrees <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calibration), "WallAlignmentTurnStepDegrees must be positive.");
        }

        if (calibration.WallAlignmentMaxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(calibration), "WallAlignmentMaxIterations must be positive.");
        }
    }
}
