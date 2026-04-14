using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Color;

namespace Zumo.Labyrinth.States;

public sealed class FollowOuterCorridorState : INavigationState
{
    private readonly INavigationAcousticService acousticService;
    private readonly IColorSensorService colorSensorService;
    private readonly INavigationMotionService motionService;
    private readonly ILedService ledService;
    private readonly ILogger<FollowOuterCorridorState> logger;
    private readonly NavigationOption config;

    public FollowOuterCorridorState(
        INavigationAcousticService acousticService,
        IColorSensorService colorSensorService,
        INavigationMotionService motionService,
        ILedService ledService,
        ILogger<FollowOuterCorridorState> logger,
        IOptions<NavigationOption> options)
    {
        this.acousticService = acousticService ?? throw new ArgumentNullException(nameof(acousticService));
        this.colorSensorService = colorSensorService ?? throw new ArgumentNullException(nameof(colorSensorService));
        this.motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public NavigationState State => NavigationState.FollowOuterCorridor;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);

        var nextState = NavigationState.FollowOuterCorridor;
        var canDriveForward = true;
        var assessment = await motionService.AssessCollisionRiskAsync();

        if (assessment == CollisionAssessment.FrontBlocked)
        {
            logger.LogDebug("Front blocked in outer corridor. Turning left.");
            await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);

            if (IsExitColorDetected())
            {
                await acousticService.PlayMarkDetectedToneAsync();
                context.TransitionTo(NavigationState.ExitLabyrinth);
                return;
            }

            await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
            await motionService.RotateInPlace(-90);
            canDriveForward = false;
        }
        else if (assessment == CollisionAssessment.SideOnlyRisk)
        {
            var resolved = await motionService.ResolveSideCollisionRiskAsync();
            if (!resolved)
            {
                logger.LogWarning("Could not resolve outer-corridor side collision risk safely.");
                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(-config.Navigation.SideRiskFallbackTurnDegrees);
                canDriveForward = false;
            }
        }

        if (canDriveForward)
        {
            await ledService.SetStatusAsync(NavigationActivityStatus.DrivingStraight);
            var stopReason = await motionService.DriveStraightSafelyAsync(config.Navigation.OuterRingScanStepMm);

            if (stopReason == SafeDriveStopReason.WallReached)
            {
                await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
                await motionService.CalibrateAgainstFrontWallAsync();

                if (IsExitColorDetected())
                {
                    await acousticService.PlayMarkDetectedToneAsync();
                    context.TransitionTo(NavigationState.ExitLabyrinth);
                    return;
                }

                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(-90);
            }

            if (stopReason == SafeDriveStopReason.CollisionRisk)
            {
                await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            }
        }

        context.TransitionTo(nextState);
    }

    private bool IsExitColorDetected()
    {
        var floorColor = colorSensorService.GetFloorColor();
        return floorColor == config.Library.OuterRingFloorColor;
    }
}
