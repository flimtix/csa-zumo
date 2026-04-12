using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library;
using Zumo.Library.Calibration;
using Zumo.Library.Color;
using Zumo.Library.Drive;
using Zumo.Library.Encoder;
using Zumo.Library.Led;
using Zumo.Library.Lidar;
using Zumo.Library.Sound;

var services = new ServiceCollection();
var navigationConfig = new NavigationOption();

services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.IncludeScopes = false;
    });
    logging.SetMinimumLevel(navigationConfig.LogLevel);
});

services.AddSingleton<IOptions<NavigationOption>>(Options.Create(navigationConfig));

services.AddSingleton<ZumoRuntime>();

services.AddSingleton<IDriveService>(serviceProvider => serviceProvider.GetRequiredService<ZumoRuntime>().Drive);
services.AddSingleton<IColorSensorService>(serviceProvider => serviceProvider.GetRequiredService<ZumoRuntime>().ColorSensor);
services.AddSingleton<IZumoRgbLedService>(serviceProvider => serviceProvider.GetRequiredService<ZumoRuntime>().RgbLed);
services.AddSingleton<ILidar>(serviceProvider => serviceProvider.GetRequiredService<ZumoRuntime>().Lidar);
services.AddSingleton<ISoundService>(serviceProvider => serviceProvider.GetRequiredService<ZumoRuntime>().Sound);

services.AddSingleton<ILidarAnalyzer, BasicLidarAnalyzer>();
services.AddSingleton<ILedService, ZumoRgbLedAdapter>();
services.AddSingleton<INavigationDistanceService, LidarDistanceService>();
services.AddSingleton<INavigationMotionService, NavigationMotionService>();
services.AddSingleton<INavigationAcousticService, NavigationAcousticService>();

services.AddSingleton<INavigationState, MoveToInnerRingState>();
services.AddSingleton<INavigationState, FollowCorridorState>();
services.AddSingleton<INavigationState, CheckGapState>();
services.AddSingleton<INavigationState, InnerRingSolvedState>();
services.AddSingleton<INavigationState, MoveToOuterRingState>();
services.AddSingleton<INavigationState, FollowOuterCorridorState>();
services.AddSingleton<INavigationState, ExitLabyrinthState>();
services.AddSingleton<INavigationState, LabyrinthSolvedState>();
services.AddSingleton<NavigationStateContext>();

services.AddSingleton<IInnerRingNavigator, InnerRingNavigator>();

using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
var navigator = serviceProvider.GetRequiredService<IInnerRingNavigator>();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var programLogger = loggerFactory.CreateLogger("Program");

programLogger.LogInformation("Configured log level: {LogLevel}", navigationConfig.LogLevel);
programLogger.LogInformation("Starting labyrinth navigation.");

ApplyCalibrationProfile(serviceProvider.GetRequiredService<ZumoRuntime>(), programLogger);

// Wait a moment for the runtime to initialize and apply calibration before starting the controller
await Task.Delay(2000);
await navigator.RunInnerRingNavigationAsync();

static void ApplyCalibrationProfile(ZumoRuntime runtime, ILogger logger)
{
    var store = new CalibrationProfileFileStore(CalibrationProfileFileStore.GetDefaultPath());
    var loaded = store.TryLoad(out var profile, out var error);
    if (!loaded)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogError("Calibration profile could not be loaded: {Error}", error);
        }
    }
    else
    {
        runtime.Encoder.SetDistanceCalibrationFactor(profile.DistanceCalibrationFactor);
        runtime.Drive.SetRotationCalibrationFactor(profile.RotationCalibrationFactor);

        logger.LogInformation(
            "Applied calibration profile: distance={DistanceCalibrationFactor}, rotation={RotationCalibrationFactor}",
            profile.DistanceCalibrationFactor,
            profile.RotationCalibrationFactor);
    }
}
