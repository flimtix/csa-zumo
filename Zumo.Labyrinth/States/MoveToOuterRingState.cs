using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zumo.Labyrinth.States;

public sealed class MoveToOuterRingState : INavigationState
{
    private readonly INavigationMotionService motionService;
    private readonly ILedService ledService;
    private readonly ILogger<MoveToOuterRingState> logger;
    private readonly NavigationOption config;

    public MoveToOuterRingState(
        INavigationMotionService motionService,
        ILedService ledService,
        ILogger<MoveToOuterRingState> logger,
        IOptions<NavigationOption> options)
    {
        this.motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public NavigationState State => NavigationState.MoveToOuterRing;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);

        var nextState = NavigationState.MoveToOuterRing;
        var assessment = await motionService.AssessCollisionRiskAsync();

        if (assessment == CollisionAssessment.FrontBlocked)
        {
            logger.LogDebug("Outer-ring entry reached front wall. Turning left to start outer corridor follow.");
            await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            await motionService.CalibrateAgainstFrontWallAsync();
            await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
            await motionService.RotateInPlace(-90);
            nextState = NavigationState.FollowOuterCorridor;
        }
        else
        {
            await ledService.SetStatusAsync(NavigationActivityStatus.DrivingStraight);
            var stopReason = await motionService.DriveStraightSafelyAsync(config.Navigation.OuterRingApproachStepMm);

            if (stopReason == SafeDriveStopReason.WallReached)
            {
                logger.LogDebug("Outer-ring entry reached front wall while driving. Turning left to start outer corridor follow.");
                await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
                await motionService.CalibrateAgainstFrontWallAsync();
                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(-90);
                nextState = NavigationState.FollowOuterCorridor;
            }

            if (stopReason == SafeDriveStopReason.CollisionRisk)
            {
                await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            }
        }

        context.TransitionTo(nextState);
    }
}
