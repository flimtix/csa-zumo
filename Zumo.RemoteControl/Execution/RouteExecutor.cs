using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library;
using Zumo.Library.Drive;
using Zumo.RemoteControl.Configuration;
using Zumo.RemoteControl.Lidar;
using Zumo.RemoteControl.Routes;
using Zumo.RemoteControl.Runtime;

namespace Zumo.RemoteControl.Execution;

public sealed class RouteExecutor : IRouteExecutor
{
    private readonly IZumoRuntimeFactory runtimeFactory;
    private readonly ILidarAnalyzer lidarAnalyzer;
    private readonly ILogger<RouteExecutor> logger;
    private readonly ILogger<LidarDistanceService> lidarLogger;
    private readonly IOptions<RemoteControlOptions> options;

    public RouteExecutor(
        IZumoRuntimeFactory runtimeFactory,
        ILidarAnalyzer lidarAnalyzer,
        ILogger<RouteExecutor> logger,
        ILogger<LidarDistanceService> lidarLogger,
        IOptions<RemoteControlOptions> options)
    {
        this.runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        this.lidarAnalyzer = lidarAnalyzer ?? throw new ArgumentNullException(nameof(lidarAnalyzer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.lidarLogger = lidarLogger ?? throw new ArgumentNullException(nameof(lidarLogger));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<RouteExecutionResult> ExecuteAsync(RouteDefinition route, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(route);

        var startedAtUtc = DateTimeOffset.UtcNow;
        var config = options.Value;

        using var runtime = runtimeFactory.Create();
        using var recorder = new UartResponseRecorder(runtime.Transport);
        var distanceService = new LidarDistanceService(runtime.Lidar, lidarAnalyzer, lidarLogger, options);

        RouteExecutionStatus executionStatus;
        try
        {
            recorder.Start();

            var pingOk = runtime.Ping.Ping(ct);
            if (pingOk)
            {
                executionStatus = await ExecuteSteps(route, runtime, distanceService, config, ct);
            }
            else
            {
                logger.LogWarning("Ping failed before route {RouteCode}.", route.Code);
                executionStatus = RouteExecutionStatus.Failed;
            }

            recorder.Stop();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Route {RouteCode} cancelled.", route.Code);
            executionStatus = RouteExecutionStatus.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Route {RouteCode} failed.", route.Code);
            executionStatus = RouteExecutionStatus.Failed;
        }

        var lines = recorder.GetRecordedLines();
        return new RouteExecutionResult(route.Code, startedAtUtc, executionStatus, lines);
    }

    private async Task<RouteExecutionStatus> ExecuteSteps(RouteDefinition route, ZumoRuntime runtime, LidarDistanceService distanceService, RemoteControlOptions config, CancellationToken ct)
    {
        foreach (RouteStep step in route.Steps)
        {
            ct.ThrowIfCancellationRequested();

            if (step.Type == RouteStepType.Drive)
            {
                var stopResult = await DriveStepAsync(step, runtime, distanceService, config, ct);
                if (stopResult != DriveStopResult.Completed)
                {
                    return stopResult switch
                    {
                        DriveStopResult.Obstacle => RouteExecutionStatus.StoppedByObstacle,
                        DriveStopResult.Timeout => RouteExecutionStatus.Timeout,
                        _ => throw new NotSupportedException($"Unknown drive stop result '{stopResult}'"),
                    };
                }
            }
            else if (step.Type == RouteStepType.Turn)
            {
                runtime.Drive.RotateInPlace(
                    (short)step.Value,
                    config.Drive.TurnSpeedMmPerSecond,
                    config.Drive.TurnAccelerationMmPerSecondSquared,
                    ct);

                await Task.Delay(config.Drive.TurnSettleDelay, ct);
            }
        }
        return RouteExecutionStatus.Completed;
    }

    private async Task<DriveStopResult> DriveStepAsync(RouteStep step, ZumoRuntime runtime, LidarDistanceService distanceService, RemoteControlOptions config, CancellationToken ct)
    {
        DriveStopResult stopResult;

        var distanceMm = step.Value;
        var frontDistance = await distanceService.GetFrontDistanceMmAsync(ct);
        if (IsPrecheckClear(frontDistance, distanceMm, config))
        {
            runtime.Drive.DriveDistance((short)distanceMm, config.Drive.DriveSpeedMmPerSecond, config.Drive.DriveAccelerationMmPerSecondSquared, ct);
            stopResult = await MonitorDriveAsync(runtime.Drive, distanceService, config, ct);
        }
        else
        {
            logger.LogInformation("Drive precheck blocked: front={FrontDistance} target={Target}.", frontDistance, distanceMm);
            runtime.Drive.SetWheelSpeed(0, 0, ct);
            stopResult = DriveStopResult.Obstacle;

        }
        return stopResult;
    }

    private static async Task<DriveStopResult> MonitorDriveAsync(IDriveService drive, ILidarDistanceService distanceService, RemoteControlOptions config, CancellationToken ct)
    {
        var timeout = config.Drive.DriveCompletionTimeout;
        var pollInterval = config.Drive.DrivePollInterval;
        var deadlineUtc = DateTime.UtcNow + timeout;

        DriveStopResult? stopResult = null;
        while (stopResult == null)
        {
            ct.ThrowIfCancellationRequested();

            short remainingDistance = drive.GetRemainingDistance(ct);
            if (Math.Abs(remainingDistance) <= config.Drive.DriveCompletionToleranceMm)
            {
                stopResult = DriveStopResult.Completed;
            }
            else
            {
                var frontDistance = await distanceService.GetFrontDistanceMmAsync(ct);
                if (frontDistance > 0 && frontDistance <= config.Safety.CollisionDangerDistanceMm)
                {
                    stopResult = DriveStopResult.Obstacle;
                }
                else if (DateTime.UtcNow >= deadlineUtc)
                {
                    stopResult = DriveStopResult.Timeout;
                }
            }

            if (!stopResult.HasValue && pollInterval > TimeSpan.Zero)
            {
                await Task.Delay(pollInterval, ct);
            }
        }

        if (stopResult != DriveStopResult.Completed)
        {
            drive.SetWheelSpeed(0, 0, ct);
        }
        return stopResult.Value;
    }

    private static bool IsPrecheckClear(int frontDistanceMm, int targetDistanceMm, RemoteControlOptions config)
    {
        var requiredClearance = targetDistanceMm + config.Safety.PrecheckDistanceMarginMm;
        if (config.Safety.CollisionDangerDistanceMm > requiredClearance)
        {
            requiredClearance = config.Safety.CollisionDangerDistanceMm;
        }
        return frontDistanceMm >= requiredClearance;
    }


    private enum DriveStopResult
    {
        Completed = 0,
        Obstacle = 1,
        Timeout = 2,
    }
}
