using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zumo.Labyrinth.Navigation.States;

public sealed class FollowCorridorState : INavigationState
{
    private readonly INavigationDistanceService distanceService;
    private readonly INavigationMotionService motionService;
    private readonly ILedService ledService;
    private readonly ILogger<FollowCorridorState> logger;
    private readonly NavigationOption config;

    public FollowCorridorState(
        INavigationDistanceService distanceService,
        INavigationMotionService motionService,
        ILedService ledService,
        ILogger<FollowCorridorState> logger,
        IOptions<NavigationOption> options)
    {
        this.distanceService = distanceService ?? throw new ArgumentNullException(nameof(distanceService));
        this.motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public NavigationState State => NavigationState.FollowCorridor;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);

        var nextState = NavigationState.FollowCorridor;
        var canDriveForward = true;
        var assessment = await motionService.AssessCollisionRiskAsync();

        if (assessment == CollisionAssessment.FrontBlocked)
        {
            logger.LogDebug("Front blocked in corridor. Performing corner turn.");
            await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            await motionService.CalibrateAgainstFrontWallAsync();
            await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
            await motionService.RotateInPlace(-90);
            canDriveForward = false;
        }

        if (assessment == CollisionAssessment.SideOnlyRisk && canDriveForward)
        {
            var resolved = await motionService.ResolveSideCollisionRiskAsync();
            if (!resolved)
            {
                logger.LogWarning("Could not resolve side collision risk safely.");
                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(-config.Navigation.SideRiskFallbackTurnDegrees);
                canDriveForward = false;
            }
        }

        if (canDriveForward)
        {
            await ledService.SetStatusAsync(NavigationActivityStatus.DrivingStraight);
            var stopReason = await motionService.DriveStraightSafelyAsync(config.Motion.StepSizeMm);

            if (stopReason == SafeDriveStopReason.DistanceReached)
            {
                var rightDistance = await distanceService.GetRightDistanceMmAsync();
                if (rightDistance > config.Navigation.GapThresholdMm)
                {
                    nextState = NavigationState.CheckGap;
                }
            }

            if (stopReason == SafeDriveStopReason.CollisionRisk)
            {
                await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            }
        }

        context.TransitionTo(nextState);
    }
}
