using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Drive;

namespace Zumo.Labyrinth.Services;

public sealed class NavigationMotionService : INavigationMotionService
{
    private readonly IDriveService driveService;
    private readonly INavigationDistanceService distanceService;
    private readonly ILogger<NavigationMotionService> logger;
    private readonly NavigationOption config;

    private int distanceDrivenSinceLastCalibrationMm;

    public NavigationMotionService(
        IDriveService driveService,
        INavigationDistanceService distanceService,
        ILogger<NavigationMotionService> logger,
        IOptions<NavigationOption> options)
    {
        this.driveService = driveService ?? throw new ArgumentNullException(nameof(driveService));
        this.distanceService = distanceService ?? throw new ArgumentNullException(nameof(distanceService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public async Task RotateInPlace(int angleDegrees)
    {
        logger.LogTrace("Turn command: angle={AngleDegrees}.", angleDegrees);

        driveService.SetWheelSpeed(-5, -5);
        driveService.RotateInPlace(
            (short)angleDegrees,
            config.Motion.TurnSpeedMmPerSecond,
            config.Motion.TurnAccelerationMmPerSecondSquared);

        await Task.Delay(config.Motion.TurnSettleDelay);
        driveService.SetWheelSpeed(0, 0);
        ResetDriftCorrection();
    }

    public async Task<SafeDriveStopReason> DriveStraightSafelyAsync(int targetDistanceMm)
    {
        var stopReason = SafeDriveStopReason.DistanceReached;
        var remainingDistanceMm = Math.Max(0, targetDistanceMm);

        while (remainingDistanceMm > 0)
        {
            var assessment = await AssessCollisionRiskAsync();
            if (assessment == CollisionAssessment.FrontBlocked)
            {
                stopReason = SafeDriveStopReason.WallReached;
                break;
            }

            if (assessment == CollisionAssessment.SideOnlyRisk)
            {
                var resolved = await ResolveSideCollisionRiskAsync();
                if (!resolved)
                {
                    stopReason = SafeDriveStopReason.CollisionRisk;
                    break;
                }
            }

            var driveChunkMm = Math.Min(remainingDistanceMm, config.Motion.SafeDriveChunkMm);
            if (driveChunkMm <= 0)
            {
                break;
            }

            DriveDistanceBlocking(driveChunkMm);
            remainingDistanceMm -= driveChunkMm;
        }

        if (config.Motion.DriveRightDriftCorrectionDegrees.HasValue)
        {
            var drivenDistanceMm = targetDistanceMm - remainingDistanceMm;
            distanceDrivenSinceLastCalibrationMm += drivenDistanceMm;

            await CorrectDriftAsync();
        }
        return stopReason;
    }

    public async Task<CollisionAssessment> AssessCollisionRiskAsync()
    {
        var snapshot = await distanceService.CaptureAsync();
        var frontBlocked = snapshot.FrontDistanceMm > 0 && snapshot.FrontDistanceMm <= config.Safety.CollisionDangerDistanceMm;
        var leftSideRisk = snapshot.FrontLeftDistanceMm > 0 && snapshot.FrontLeftDistanceMm <= config.Safety.SideCollisionDangerDistanceMm;
        var rightSideRisk = snapshot.FrontRightDistanceMm > 0 && snapshot.FrontRightDistanceMm <= config.Safety.SideCollisionDangerDistanceMm;

        var assessment = CollisionAssessment.NoRisk;
        if (frontBlocked)
        {
            assessment = CollisionAssessment.FrontBlocked;
        }
        else if (leftSideRisk || rightSideRisk)
        {
            assessment = CollisionAssessment.SideOnlyRisk;
        }

        logger.LogDebug(
            "Collision assessment: front={FrontDistanceMm} left={LeftDistanceMm} right={RightDistanceMm} assessment={Assessment}",
            snapshot.FrontDistanceMm,
            snapshot.FrontLeftDistanceMm,
            snapshot.FrontRightDistanceMm,
            assessment);

        return assessment;
    }

    public async Task<bool> ResolveSideCollisionRiskAsync()
    {
        var resolved = false;
        var abort = false;

        for (var correction = 1;
             correction <= config.Safety.GapAlignmentMaxCorrections && !resolved && !abort;
             correction++)
        {
            var snapshot = await distanceService.CaptureAsync();
            var leftSideRisk = snapshot.FrontLeftDistanceMm > 0
                && snapshot.FrontLeftDistanceMm <= config.Safety.SideCollisionDangerDistanceMm;
            var rightSideRisk = snapshot.FrontRightDistanceMm > 0
                && snapshot.FrontRightDistanceMm <= config.Safety.SideCollisionDangerDistanceMm;

            if (!leftSideRisk && !rightSideRisk)
            {
                resolved = true;
            }
            else
            {
                var correctionAngle = DetermineSideAlignmentCorrectionAngle(snapshot.FrontLeftDistanceMm, snapshot.FrontRightDistanceMm);

                if (correctionAngle == 0)
                {
                    abort = true;
                }
                else
                {
                    logger.LogTrace("Side-risk correction {Correction}/{MaxCorrections}: angle={CorrectionAngle}.", correction, config.Safety.GapAlignmentMaxCorrections, correctionAngle);

                    await RotateInPlace(correctionAngle);

                    if (config.Safety.GapAlignmentRecheckDelayMs > 0)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(config.Safety.GapAlignmentRecheckDelayMs));
                    }

                    var updatedAssessment = await AssessCollisionRiskAsync();
                    if (updatedAssessment == CollisionAssessment.NoRisk)
                    {
                        resolved = true;
                    }

                    if (updatedAssessment == CollisionAssessment.FrontBlocked)
                    {
                        abort = true;
                    }
                }
            }
        }

        return resolved;
    }

    public async Task CalibrateAgainstFrontWallAsync()
    {
        logger.LogInformation("Wall calibration started.");
        await ApproachWallForCalibrationAsync();
        await AlignParallelToWallAsync();
        ResetDriftCorrection();
        logger.LogInformation("Wall calibration finished.");
    }

    private async Task ApproachWallForCalibrationAsync()
    {
        var frontDistance = await distanceService.GetFrontDistanceMmAsync();
        var iteration = 0;
        var stopApproach = false;

        while (frontDistance > config.Calibration.WallCalibrationDistanceMm && !stopApproach)
        {
            var remaining = frontDistance - config.Calibration.WallCalibrationDistanceMm;
            var step = Math.Min(remaining, config.Calibration.WallCalibrationApproachStepMm);

            if (step <= 0)
            {
                stopApproach = true;
            }
            else
            {
                logger.LogTrace("Calibration approach iteration {Iteration}: front={FrontDistanceMm} step={StepMm}.", iteration, frontDistance, step);

                DriveDistanceBlocking(step);
                frontDistance = await distanceService.GetFrontDistanceMmAsync();
                iteration++;

                if (iteration > 20)
                {
                    logger.LogWarning("Calibration approach reached iteration limit.");
                    stopApproach = true;
                }
            }
        }
    }

    private async Task AlignParallelToWallAsync()
    {
        var completed = false;

        for (var iteration = 0;
             iteration < config.Calibration.WallAlignmentMaxIterations && !completed;
             iteration++)
        {
            var snapshot = await distanceService.CaptureAsync();
            var leftDistance = snapshot.FrontLeftDistanceMm;
            var rightDistance = snapshot.FrontRightDistanceMm;

            if (leftDistance <= 0 || rightDistance <= 0)
            {
                logger.LogWarning("Wall alignment skipped due to invalid side distances (left={LeftDistanceMm}, right={RightDistanceMm}).", leftDistance, rightDistance);
            }
            else
            {
                var delta = leftDistance - rightDistance;
                logger.LogTrace("Wall alignment iteration {Iteration}: left={LeftDistanceMm} right={RightDistanceMm} delta={DeltaMm}.", iteration, leftDistance, rightDistance, delta);

                var correctionAngle = config.Calibration.WallAlignmentTurnStepDegrees * (delta > 0 ? 1 : -1);
                await RotateInPlace(correctionAngle);

                if (Math.Abs(delta) <= config.Calibration.WallAlignmentToleranceMm)
                {
                    completed = true;
                }
            }
        }

        if (!completed)
        {
            logger.LogWarning("Wall alignment reached max iterations without convergence.");
        }
    }

    private void DriveDistanceBlocking(int distanceMm)
    {
        logger.LogTrace("Drive command: distance={DistanceMm}.", distanceMm);

        driveService.DriveDistance(
            (short)distanceMm,
            config.Motion.DriveSpeedMmPerSecond,
            config.Motion.DriveAccelerationMmPerSecondSquared);

        var completed = driveService.WaitForDistanceCompletion(
            timeout: config.Motion.DriveCompletionTimeout,
            pollInterval: config.Motion.DrivePollInterval);

        driveService.SetWheelSpeed(0, 0);

        if (!completed)
        {
            throw new TimeoutException($"Drive command timed out after {config.Motion.DriveCompletionTimeout}.");
        }
    }

    private int DetermineSideAlignmentCorrectionAngle(int leftDistance, int rightDistance)
    {
        var correctionAngle = 0;
        var leftSideRisk = leftDistance > 0 && leftDistance <= config.Safety.SideCollisionDangerDistanceMm;
        var rightSideRisk = rightDistance > 0 && rightDistance <= config.Safety.SideCollisionDangerDistanceMm;

        if (leftSideRisk && !rightSideRisk)
        {
            correctionAngle = config.Safety.GapAlignmentCorrectionDegrees;
        }

        if (rightSideRisk && !leftSideRisk)
        {
            correctionAngle = -config.Safety.GapAlignmentCorrectionDegrees;
        }

        if (leftSideRisk && rightSideRisk)
        {
            if (leftDistance < rightDistance)
            {
                correctionAngle = config.Safety.GapAlignmentCorrectionDegrees;
            }

            if (rightDistance < leftDistance)
            {
                correctionAngle = -config.Safety.GapAlignmentCorrectionDegrees;
            }
        }

        return correctionAngle;
    }

    private async Task CorrectDriftAsync()
    {
        if (config.Motion.DriveRightDriftCorrectionDegrees.HasValue && Math.Abs(distanceDrivenSinceLastCalibrationMm) >= 300)
        {
            logger.LogInformation("Performing drift correction after driving {DistanceDrivenMm}mm since last calibration.", distanceDrivenSinceLastCalibrationMm);
            var drivingDirectionFactor = distanceDrivenSinceLastCalibrationMm >= 0 ? 1 : -1;
            var correctionDegrees = config.Motion.DriveRightDriftCorrectionDegrees.Value;
            var relativeCorrection = correctionDegrees * drivingDirectionFactor;
            await RotateInPlace(relativeCorrection);
            ResetDriftCorrection();
        }
        else
        {
            driveService.SetWheelSpeed(-5, -5);
            driveService.RotateInPlace(1, config.Motion.TurnSpeedMmPerSecond, config.Motion.TurnAccelerationMmPerSecondSquared);
            await Task.Delay(100);
            driveService.SetWheelSpeed(0, 0);
        }
    }

    private void ResetDriftCorrection()
    {
        distanceDrivenSinceLastCalibrationMm = 0;
    }
}
