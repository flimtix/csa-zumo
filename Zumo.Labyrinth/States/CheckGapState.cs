using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Color;

namespace Zumo.Labyrinth.States;

public sealed class CheckGapState : INavigationState
{
    private const ushort DRIVE_FOREVER = 9999;
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
}
