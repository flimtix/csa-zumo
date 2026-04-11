using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zumo.Labyrinth.States;

public sealed class MoveToInnerRingState : INavigationState
{
    private readonly INavigationDistanceService distanceService;
    private readonly INavigationMotionService motionService;
    private readonly ILedService ledService;
    private readonly ILogger<MoveToInnerRingState> logger;
    private readonly NavigationOption config;

    public MoveToInnerRingState(
        INavigationDistanceService distanceService,
        INavigationMotionService motionService,
        ILedService ledService,
        ILogger<MoveToInnerRingState> logger,
        IOptions<NavigationOption> options)
    {
        this.distanceService = distanceService ?? throw new ArgumentNullException(nameof(distanceService));
        this.motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public NavigationState State => NavigationState.MoveToInnerRing;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);

        var distanceSnapshot = await distanceService.CaptureAsync();

        NavigationState nextState;
        if (distanceSnapshot.RightDistanceMm > config.Navigation.InnerRingEntryWallMissingMm)
        {
            logger.LogDebug("Side walls are missing. Switching to corridor mode.");

            await ledService.SetStatusAsync(NavigationActivityStatus.DrivingStraight);
            await motionService.DriveStraightSafelyAsync(config.Navigation.InnerRingEntryDriveInMm);
            await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
            await motionService.RotateInPlace(-90);

            nextState = NavigationState.FollowCorridor;
        }
        else if (distanceSnapshot.FrontDistanceMm > config.Navigation.WallThresholdMm)
        {
            await ledService.SetStatusAsync(NavigationActivityStatus.DrivingStraight);
            await motionService.DriveStraightSafelyAsync(config.Motion.StepSizeMm);
            nextState = NavigationState.MoveToInnerRing;
        }
        else
        {
            await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
            await motionService.CalibrateAgainstFrontWallAsync();
            await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
            await motionService.RotateInPlace(-90);
            nextState = NavigationState.FollowCorridor;
        }

        context.TransitionTo(nextState);
    }
}
