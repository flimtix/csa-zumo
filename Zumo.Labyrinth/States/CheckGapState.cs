using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Color;

namespace Zumo.Labyrinth.States;

public sealed class CheckGapState : INavigationState
{
    private const int DRIVE_FOREVER = 999;
    private readonly INavigationAcousticService acousticService;
    private readonly IColorSensorService colorSensorService;
    private readonly INavigationDistanceService distanceService;
    private readonly INavigationMotionService motionService;
    private readonly ILedService ledService;
    private readonly ILogger<CheckGapState> logger;
    private readonly NavigationOption config;

    public CheckGapState(
        INavigationAcousticService acousticService,
        IColorSensorService colorSensorService,
        INavigationDistanceService distanceService,
        INavigationMotionService motionService,
        ILedService ledService,
        ILogger<CheckGapState> logger,
        IOptions<NavigationOption> options)
    {
        this.acousticService = acousticService ?? throw new ArgumentNullException(nameof(acousticService));
        this.colorSensorService = colorSensorService ?? throw new ArgumentNullException(nameof(colorSensorService));
        this.distanceService = distanceService ?? throw new ArgumentNullException(nameof(distanceService));
        this.motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public NavigationState State => NavigationState.CheckGap;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);

        var nextState = NavigationState.FollowCorridor;
        var floorColor = colorSensorService.GetFloorColor();
        var isCorrectColor = floorColor == config.Library.InnerRingFloorColor;

        if (isCorrectColor)
        {
            await acousticService.PlayMarkDetectedToneAsync();

            // IDEA: If the paper is as big as the gap, we drive till we stop seeing the paper, then turn around and drive half the distance back, which should put us in the middle of the gap.
            // Then we align ourselv to the inner wall and make sure to drive straight through the gap.
            // In the presentation the paper was so small that we could just turn 90 degrees and drive straight.
            // But in the actual labyrinth the paper is quite big, so we might need to do some alignment to make sure we are in the middle of the gap and not too close to one of the walls.
            //
            // await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);
            // await AlignGapCenterAsync();

            // await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            // await motionService.RotateInPlace(90);
            // await motionService.CalibrateAgainstFrontWallAsync();

            // await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
            // await TrunAroundAsync();

            await Task.Delay(config.Motion.TurnSettleDelay);
            await motionService.RotateInPlace(90);
            await Task.Delay(config.Motion.TurnSettleDelay);

            await ledService.SetStatusAsync(NavigationActivityStatus.DrivingStraight);

            var stopReason = SafeDriveStopReason.CollisionRisk;
            for (int attempt = 1; attempt <= config.Navigation.GapDetectionMaxAttempts; attempt++)
            {
                stopReason = await motionService.DriveStraightSafelyAsync(DRIVE_FOREVER);
                if (stopReason == SafeDriveStopReason.CollisionRisk)
                {
                    logger.LogWarning("Gap check attempt {Attempt}: collision risk detected while driving straight.", attempt);
                    await motionService.ResolveSideCollisionRiskAsync();
                }
                else
                {
                    nextState = NavigationState.InnerRingSolved;
                    break;
                }
            }

            if (stopReason == SafeDriveStopReason.CollisionRisk)
            {
                logger.LogWarning("Gap check failed after {Attempt} attempts due to persistent collision risk.", config.Navigation.GapDetectionMaxAttempts);
                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(-90);
            }
        }

        context.TransitionTo(nextState);
    }

    private async Task AlignGapCenterAsync()
    {
        int countDriven = 0;
        while (countDriven < config.Navigation.GapDetectionMaxAttempts)
        {
            var assessment = await motionService.DriveStraightSafelyAsync(config.Navigation.GapCheckDriveInMm);
            if (assessment == SafeDriveStopReason.WallReached)
            {
                logger.LogWarning("Gap alignment attempt {Attempt}: stopped by wall reached.", countDriven + 1);
                break;
            }
            else if (assessment == SafeDriveStopReason.CollisionRisk)
            {
                var couldResolve = await motionService.ResolveSideCollisionRiskAsync();
                if (!couldResolve)
                {
                    logger.LogWarning("Gap alignment attempt {Attempt}: could not resolve side collision risk.", countDriven + 1);
                    break;
                }
            }
            countDriven++;

            var floorColor = colorSensorService.GetFloorColor();
            var isCorrectColor = floorColor == config.Library.InnerRingFloorColor;

            if (!isCorrectColor)
            {
                logger.LogWarning("Gap alignment attempt {Attempt}: incorrect floor color detected.", countDriven);
                break;
            }
        }

        await TrunAroundAsync();
        for (int i = 1; i <= countDriven / 2; i++)
        {
            await motionService.DriveStraightSafelyAsync(config.Navigation.GapCheckDriveInMm);

            var rightDistance = await distanceService.GetRightDistanceMmAsync();
            if (rightDistance <= 0)
            {
                logger.LogWarning("Gap alignment confirmation attempt {Attempt}: invalid right distance reading.", i);
            }
        }
    }

    private async Task TrunAroundAsync()
    {
        // 180 degree does not work as it only turns 70-80 degrees, so we do two 90 degree turns :)
        await motionService.RotateInPlace(90);
        await Task.Delay(config.Motion.TurnSettleDelay);
        await motionService.RotateInPlace(90);
        await Task.Delay(config.Motion.TurnSettleDelay);
        // await motionService.RotateInPlace(-5);
    }
}
