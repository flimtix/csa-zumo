using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zumo.Labyrinth.States;

public sealed class ExitLabyrinthState : INavigationState
{
    private readonly INavigationMotionService motionService;
    private readonly ILedService ledService;
    private readonly ILogger<ExitLabyrinthState> logger;
    private readonly NavigationOption config;

    public ExitLabyrinthState(
        INavigationMotionService motionService,
        ILedService ledService,
        ILogger<ExitLabyrinthState> logger,
        IOptions<NavigationOption> options)
    {
        this.motionService = motionService ?? throw new ArgumentNullException(nameof(motionService));
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public NavigationState State => NavigationState.ExitLabyrinth;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await ledService.SetStatusAsync(NavigationActivityStatus.ExitingLabyrinth);

        await motionService.CalibrateAgainstFrontWallAsync();
        await motionService.RotateInPlace(90);

        var remainingDistanceMm = config.Navigation.OuterRingExitDistanceMm;
        var retryCount = 0;

        while (remainingDistanceMm > 0)
        {
            var segmentMm = Math.Min(config.Motion.SafeDriveChunkMm, remainingDistanceMm);
            var stopReason = await motionService.DriveStraightSafelyAsync(segmentMm);

            if (stopReason == SafeDriveStopReason.DistanceReached)
            {
                remainingDistanceMm -= segmentMm;
                continue;
            }

            retryCount++;
            logger.LogWarning(
                "Exit drive interrupted with {StopReason}. Retry {Retry}/{MaxRetries}.",
                stopReason,
                retryCount,
                config.Navigation.OuterRingExitMaxRetries);

            if (retryCount > config.Navigation.OuterRingExitMaxRetries)
            {
                logger.LogError("Exit drive aborted after retry limit. Returning to outer corridor follow.");
                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(-90);
                await ledService.SetStatusAsync(NavigationActivityStatus.Blocked);
                context.TransitionTo(NavigationState.FollowOuterCorridor);
                return;
            }

            var resolved = await motionService.ResolveSideCollisionRiskAsync();
            if (!resolved)
            {
                await ledService.SetStatusAsync(NavigationActivityStatus.Turning);
                await motionService.RotateInPlace(config.Navigation.SideRiskFallbackTurnDegrees);
                await motionService.RotateInPlace(-config.Navigation.SideRiskFallbackTurnDegrees);
            }

            if (config.Navigation.OuterRingExitRetryDelayMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(config.Navigation.OuterRingExitRetryDelayMs));
            }

            await ledService.SetStatusAsync(NavigationActivityStatus.ExitingLabyrinth);
        }

        context.TransitionTo(NavigationState.LabyrinthSolved);
    }
}
