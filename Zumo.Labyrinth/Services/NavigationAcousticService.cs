using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zumo.Library.Sound;

namespace Zumo.Labyrinth.Services;

public sealed class NavigationAcousticService : INavigationAcousticService
{
    private readonly ISoundService soundService;
    private readonly ILogger<NavigationAcousticService> logger;
    private readonly NavigationOption config;

    public NavigationAcousticService(
        ISoundService soundService,
        ILogger<NavigationAcousticService> logger,
        IOptions<NavigationOption> options)
    {
        this.soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        config = options.Value ?? throw new ArgumentException("Navigation options must not be null.", nameof(options));
    }

    public ValueTask PlayMarkDetectedToneAsync()
    {
        QueuePlayback(
            () => soundService.Beep(
                config.Acoustic.MarkerDetectedFrequencyHz,
                config.Acoustic.MarkerDetectedDurationMs,
                CancellationToken.None),
            "mark-detected");

        return ValueTask.CompletedTask;
    }

    public ValueTask PlayMazeSolvedMelodyAsync()
    {
        QueuePlayback(
            () => soundService.Play(config.Acoustic.MazeSolvedMelody, CancellationToken.None),
            "maze-solved");

        return ValueTask.CompletedTask;
    }

    private void QueuePlayback(Action playbackAction, string acousticEventName)
    {
        if (!config.Acoustic.EnableAcousticFeedback)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                playbackAction();
                logger.LogDebug("Acoustic event queued: {AcousticEventName}", acousticEventName);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Acoustic event failed: {AcousticEventName}", acousticEventName);
            }
        });
    }
}
