using Microsoft.Extensions.Logging;

namespace Zumo.Labyrinth.States;

public sealed class InnerRingSolvedState : INavigationState
{
    private readonly ILogger<InnerRingSolvedState> logger;
    private readonly ILedService ledService;

    public InnerRingSolvedState(
        ILedService ledService,
        ILogger<InnerRingSolvedState> logger)
    {
        this.ledService = ledService ?? throw new ArgumentNullException(nameof(ledService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public NavigationState State => NavigationState.InnerRingSolved;

    public async Task ExecuteAsync(NavigationStateContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        logger.LogInformation("Inner-ring navigation completed. Transitioning to outer-ring phase.");
        await ledService.SetStatusAsync(NavigationActivityStatus.Scanning);
        context.TransitionTo(NavigationState.MoveToOuterRing);
    }
}
